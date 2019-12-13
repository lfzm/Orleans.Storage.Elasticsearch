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
        private readonly ElasticsearchStorageInfo _storageInfo;
        private readonly ElasticsearchStorageOptions _options;
        private readonly IElasticsearchStorage _storage;
        public Compensater(ILogger<Compensater> logger)
        {
            this._logger = logger;
            this._storageInfo = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageInfo>(this.GetPrimaryKeyString());
            this._options = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageOptions>(_storageInfo.StorageName);
            this._storage = (IElasticsearchStorage)this.ServiceProvider.GetRequiredService(typeof(IElasticsearchStorage<>).MakeGenericType(_storageInfo.ModelType));
        }

        public override async Task OnActivateAsync()
        {
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
                this.DeactivateOnIdle();
            }
        }
        private async Task CheckCompleta()
        {
            int count = 100;
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
                    this._logger.LogDebug($"completa check synced {count} count data");
                    if (count == -1)
                        this._logger.LogError($"completa check synced index Elasticsearch failed");
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "completa check failed");
                }
            }

        }
    }
}
