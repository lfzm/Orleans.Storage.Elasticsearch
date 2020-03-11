using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public class Compensater : Grain, ICompensater, IRemindable
    {
        private const string COMPLETECHECK = "CompleteCheck";
        private readonly ILogger _logger;
        private IGrainReminder _reminder;
        private ElasticsearchStorageInfo _storageInfo;
        public Compensater(ILogger<Compensater> logger)
        {
            this._logger = logger;
        }
   
        public async Task CompensateAsync(CompensateData data)
        {
            this._storageInfo = this.GetElasticsearchStorageInfo(data.IndexName);
            await this.RegisterOrUpdateReminder(data.ToString(), TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
        }

        public async Task CompletaCheckAsync()
        {
            if (_reminder != null)
                return;
            this._storageInfo = this.GetElasticsearchStorageInfo(this.GetPrimaryKeyString());
            if (!_storageInfo.CompleteCheck)
                return;
            // 定时检查数据完整度
            TimeSpan dueTime = _storageInfo.CheckInterval;
            if (_storageInfo.CheckStartTime > DateTime.Now)
                dueTime = _storageInfo.CheckStartTime - DateTime.Now;
            this._logger.LogDebug($"{_storageInfo.IndexName} CheckStartTime:{_storageInfo.CheckStartTime} CheckInterval:{_storageInfo.CheckInterval} start complete check");
            _reminder = await this.RegisterOrUpdateReminder(COMPLETECHECK, dueTime, _storageInfo.CheckInterval);
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (reminderName.Equals(COMPLETECHECK))
                await this.CheckCompleta();
            else
                await this.Compensate(CompensateData.From(reminderName));
            this.DeactivateOnIdle();
        }
        private async Task Compensate(CompensateData data)
        {
            var _storage = (IElasticsearchStorage)this.ServiceProvider.GetRequiredService(typeof(IElasticsearchStorage<>)
                .MakeGenericType(_storageInfo.ModelType));
            this._logger.LogDebug($"Start data compensation: {data.ToString()}");
            if (data.Type == CompensateType.Clear)
            {
                if (!await _storage.DeleteAsync(data.Id))
                {
                    this._logger.LogInformation($"{_storageInfo.IndexName} {data.Id} Compensate clear filed");
                    return;
                }
            }
            else
            {
                if (!await _storage.RefreshAsync(data.Id))
                {
                    this._logger.LogInformation($"{_storageInfo.IndexName} {data.Id} Compensate refresh filed");
                    return;
                }
            }
            this._logger.LogDebug("Data compensation succeeded, stop timer");
            var reminder = await this.GetReminder(data.ToString());
            if (reminder != null)
            {
                await this.UnregisterReminder(reminder);
            }
        }

        private async Task CheckCompleta()
        {
            if (!_storageInfo.Compensate)
                return;
            this._logger.LogDebug($"Start sanity check: {this._storageInfo.IndexName}");
            var compensateComplete = false;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            // 循环到所有数据全部完成
            var _options = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageOptions>(_storageInfo.StorageName);
            var _storage = (IElasticsearchStorage)this.ServiceProvider.GetRequiredService(typeof(IElasticsearchStorage<>)
                .MakeGenericType(_storageInfo.ModelType));
            while (compensateComplete == false)
            {
                // 执行时间超过30分钟，暂停检查
                if (watch.Elapsed >= _options.CompleteCheckTimeOut)
                {
                    watch.Stop();
                    break;
                }
                try
                {
                    compensateComplete = await _storage.CompensateSync();
                    if (!compensateComplete)
                        await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    watch.Stop();
                    this._logger.LogError(ex, $"{this._storageInfo.IndexName} completa check failed");
                    break;
                }
            }
        }

        private ElasticsearchStorageInfo GetElasticsearchStorageInfo(string indexName)
        {
            return this.ServiceProvider.GetOptionsByName<ElasticsearchStorageInfo>(indexName);
        }
    }
}
