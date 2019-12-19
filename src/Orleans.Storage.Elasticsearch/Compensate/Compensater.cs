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
        private ElasticsearchStorageInfo _storageInfo;
        private ElasticsearchStorageOptions _options;
        private IElasticsearchStorage _storage;
        public Compensater(ILogger<Compensater> logger)
        {
            this._logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            this._storageInfo = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageInfo>(this.GetPrimaryKeyString());
            this._options = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageOptions>(_storageInfo.StorageName);
            this._storage = (IElasticsearchStorage)this.ServiceProvider.GetRequiredService(typeof(IElasticsearchStorage<>).MakeGenericType(_storageInfo.ModelType));

            if (_storageInfo.CompleteCheck)
            {
                // 定时检查数据完整度
                TimeSpan dueTime = _storageInfo.CheckStartTime - DateTime.Now;
                await this.RegisterOrUpdateReminder(COMPLETECHECK, dueTime, _storageInfo.CheckInterval);
            }
            await base.OnActivateAsync();
        }
        public async Task CompensateAsync(CompensateData data)
        {
            await this.RegisterOrUpdateReminder(data.ToString(), TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
        }
        public Task CompletaCheckAsync()
        {
            return Task.CompletedTask;
        }
        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (reminderName.Equals(COMPLETECHECK))
                await this.CheckCompleta();
            else
                await this.Compensate(CompensateData.From(reminderName));
        }
        private async Task Compensate(CompensateData data)
        {
            if (data.Type == CompensateType.Clear)
            {
                if (!await this._storage.DeleteAsync(data.Id))
                {
                    this._logger.LogInformation($"{_storageInfo.IndexName} {data.Id} Compensate clear filed");
                    return;
                }
            }
            else
            {
                if (!await this._storage.RefreshAsync(data.Id))
                {
                    this._logger.LogInformation($"{_storageInfo.IndexName} {data.Id} Compensate refresh filed");
                    return;
                }
            }
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

            int count = 0;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            // 循环到所有数据全部完成
            while (count == int.MinValue)
            {
                // 执行时间超过30分钟，暂停检查
                if (watch.Elapsed == _options.CompleteCheckTimeOut)
                {
                    watch.Stop();
                    break;
                }
                try
                {
                    await Task.Delay(10);
                    count = await _storage.CompensateSync();
                    this._logger.LogInformation($"completa check synced {count} count data");
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "completa check failed");
                }
            }

        }
    }
}
