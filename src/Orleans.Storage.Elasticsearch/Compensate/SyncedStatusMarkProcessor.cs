using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public class SyncedStatusMarkProcessor : ISyncedStatusMarkProcessor
    {
        private readonly ILogger _logger;
        private readonly string indexName;
        private readonly IServiceProvider _serviceProvider;
        private readonly ElasticsearchStorageOptions _options;
        private readonly ConcurrentQueue<string> idqueue = new ConcurrentQueue<string>();
        private readonly ElasticsearchStorageInfo _storageInfo;
        private DateTime lastMarkTime = DateTime.MinValue;
        private bool AllMark = false; // 是否清空队列中所有的待标记Id
        private int isProcessing = 0;

        public SyncedStatusMarkProcessor(IServiceProvider serviceProvider, string indexName)
        {
            this.indexName = indexName;
            this._storageInfo = serviceProvider.GetOptionsByName<ElasticsearchStorageInfo>(indexName);
            this._logger = serviceProvider.GetRequiredService<ILogger<SyncedStatusMarkProcessor>>();
            this._options = serviceProvider.GetOptionsByName<ElasticsearchStorageOptions>(_storageInfo.StorageName);
            this._serviceProvider = serviceProvider;
        }

        public void MarkSynced(string id)
        {
            if (!_storageInfo.CompleteCheck)
                return;
            idqueue.Enqueue(id);
            // 当堆积数量超过100 或者时间超过10分钟，启动标记已同步
            if (idqueue.Count > _options.MarkProcessMaxCount || DateTime.Now - lastMarkTime >= _options.MarkWaitInterval)
                this.Processor();
        }

        public async void Processor()
        {
            if (Interlocked.CompareExchange(ref isProcessing, 1, 0) == 1)
                return;
            var ids = new List<string>();
            while (idqueue.Count > 0)
            {
                try
                {
                    ids.Clear();
                    // 循环取出队列中的数据
                    while (idqueue.TryDequeue(out string id))
                    {
                        ids.Add(id);
                        if (ids.Count >= _options.MarkProcessMaxCount)
                            break;
                    }
                    using var storage = (ICompensateCheckStorage)this._serviceProvider.GetRequiredService(typeof(ICompensateStorage<>).MakeGenericType(_storageInfo.ModelType));
                    await storage.ModifySyncedStatus(ids);
                    await Task.Delay(1);
                    this.lastMarkTime = DateTime.Now;
                    // 当队列中的数量小于 MAXPROCESSCOUNT 等待下次处理
                    if (idqueue.Count < _options.MarkProcessMaxCount && !AllMark)
                        break;
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"mark  {this.indexName}  synced Elasticsearch failed ");
                }
            }
            Interlocked.Exchange(ref isProcessing, 0);
        }

        /// <summary>
        /// 清空挤压的所有的Mark
        /// </summary>
        /// <returns></returns>
        public async Task WaitMarkComplete()
        {
            this.AllMark = true;
            this.Processor();
            // 等待全部处理完成
            while (idqueue.Count > 0)
            {
                await Task.Delay(1);
            }
            this.AllMark = false;
        }

        public int WaitCount
        {
            get { return this.idqueue.Count; }
        }
    }
}
