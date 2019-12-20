using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Storage.Elasticsearch.Compensate;
using Orleans.Storage.Elasticsearch.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch Storage
    /// </summary>
    /// <typeparam name="TModel">Elasticsearch 存储对象（有标记Mapping映射）</typeparam>
    public class ElasticsearchStorage<TModel> : IElasticsearchStorage<TModel>
        where TModel : class, IElasticsearchModel
    {
        private readonly IElasticsearchClient<TModel> _client;
        private readonly IDataflowBufferBlock<TModel> _dataflowBuffer;
        private readonly ILogger _logger;

        protected readonly IServiceProvider ServiceProvider;
        protected readonly ElasticsearchStorageInfo _storageInfo;
        protected readonly ISyncedStatusMarkProcessor _syncedMarkProcessor;
        protected readonly ElasticsearchStorageOptions _options;

        public ElasticsearchStorage(IServiceProvider serviceProvider, string indexName, IElasticsearchClient<TModel> client)
            : this(serviceProvider, indexName)
        {
            this._client = client;
            this._dataflowBuffer = new DataflowBufferBlock<TModel>(this.IndexMany);
            this._logger = this.ServiceProvider.GetRequiredService<ILogger<ElasticsearchStorage<TModel>>>();
        }

        public ElasticsearchStorage(IServiceProvider serviceProvider, string indexName)
        {
            this.ServiceProvider = serviceProvider;
            this._storageInfo = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageInfo>(indexName);
            this._options = this.ServiceProvider.GetOptionsByName<ElasticsearchStorageOptions>(this._storageInfo.StorageName);
            this._syncedMarkProcessor = this.ServiceProvider.GetServiceByName<ISyncedStatusMarkProcessor>(indexName);
        }

        public virtual Task<TModel> GetAsync(string id)
        {
            return _client.GetAsync(id);
        }
        public virtual Task<IEnumerable<TModel>> GetListAsync(IEnumerable<string> ids)
        {
            return _client.GetListAsync(ids);
        }

        public virtual async Task<bool> DeleteAsync(string id)
        {
            if (await this._client.DeleteAsync(id))
            {
                this._syncedMarkProcessor?.MarkSynced(id);
                return true;
            }
            else
            {
                await this.CompensateAsync(id, CompensateType.Clear); //数据补偿
                return false;
            }
        }
        public virtual async Task<bool> IndexAsync(TModel model)
        {
            if (await this._dataflowBuffer.SendAsync(model))
            {
                this._syncedMarkProcessor?.MarkSynced(model.GetPrimaryKey());
                return true;
            }
            else
            {
                await this.CompensateAsync(model.GetPrimaryKey(), CompensateType.Write); //数据补偿
                return false;
            }
        }
        public virtual async Task IndexManyAsync(IEnumerable<TModel> modelList)
        {
            var tasks = modelList.Select(f => this.IndexAsync(f));
            await Task.WhenAll(tasks.ToArray());
        }
        public virtual async Task DeleteManyAsync(IEnumerable<string> ids)
        {
            var tasks = ids.Select(f => this.DeleteAsync(f));
            await Task.WhenAll(tasks.ToArray());
        }
        async Task<object> IElasticsearchStorage.GetAsync(string id)
        {
            return await this.GetAsync(id);
        }
        async Task<IEnumerable<object>> IElasticsearchStorage.GetListAsync(IEnumerable<string> ids)
        {
            return await this.GetListAsync(ids);
        }
        public virtual Task<bool> IndexAsync(object model)
        {
            if (model is TModel d)
                return this.IndexAsync(d);
            else
                throw new Exception($"{model.GetType().FullName} must be of type {typeof(TModel).FullName}");
        }
        public virtual Task IndexManyAsync(IEnumerable<object> modelList)
        {
            var datas = modelList.Select(data =>
            {
                if (data is TModel d)
                {
                    return d;
                }
                else
                    throw new Exception($"{data.GetType().FullName} must be of type {typeof(TModel).FullName}");
            });
            return this.IndexManyAsync(datas);
        }
        public virtual async Task IndexMany(BufferBlock<IDataflowBufferWrap<TModel>> bufferBlock)
        {
            List<IDataflowBufferWrap<TModel>> modelWraps = new List<IDataflowBufferWrap<TModel>>();
            while (bufferBlock.TryReceive(out var wrap))
            {
                // 一次操作数据不能超过配置限制
                if (modelWraps.Count > this._options.IndexManyMaxCount)
                    break;
                modelWraps.Add(wrap);
            }
            var docs = modelWraps.Select(f =>
            {
                if (f.Data is IElasticsearchConcurrencyModel model)
                    return new ElasticsearchDocument<TModel>(f.Data, f.Data.GetPrimaryKey(), model.GetVersionNo());
                else
                    return new ElasticsearchDocument<TModel>(f.Data, f.Data.GetPrimaryKey());
            });
            var response = await this._client.IndexManyAsync(docs);
            if (response.IsValid)
                modelWraps.ForEach(f => f.CompleteHandler(true));// 执行成功全部返回成功
            else
            {
                // 部分执行失败，检查是否成功并且返回
                modelWraps.ForEach(w =>
                {
                    string id = w.Data.GetPrimaryKey();
                    var r = response.Items.FirstOrDefault(f => f.Id == id);
                    if (!r.IsValid)
                    {
                        // 如果有版本冲突，默认成功执行
                        if (r.Status == 409 && r.Error.Reason.Contains("version conflict"))
                        {
                            this._logger.LogInformation($"{id} Elasticsearch index version conflict");
                            w.CompleteHandler(true);
                            return;
                        }
                    }
                    w.CompleteHandler(r.IsValid);
                });
            }
        }
        protected async Task CompensateAsync(string id, CompensateType type)
        {
            if (!this._storageInfo.Compensate)
                return;
            var reminderTable = this.ServiceProvider.GetService<IReminderTable>();
            if (reminderTable != null)
            {
                await this.ServiceProvider.GetRequiredService<IGrainFactory>()
                         .GetGrain<ICompensater>(this._storageInfo.IndexName)
                         .CompensateAsync(new CompensateData(id, type));
            }
        }
        public async Task<bool> RefreshAsync(string id)
        {
            // 调用补偿仓储获取数据
            var model = await this.GetToDbAsync(id);
            if (model == null)
                return false;
            return await this.IndexAsync(model); // 更新到Elasticsearch中去
        }
        public async Task<object> GetToDbAsync(string Id)
        {
            var storage = this.ServiceProvider.GetRequiredService<ICompensateStorage<TModel>>();
            return await storage.GetAsync(Id);
        }
        public async Task<int> CompensateSync()
        {
            if (!this._storageInfo.CompleteCheck)
                throw new Exception($"The {nameof(TModel)}  must implement the Orleans.Storage.Elasticsearch.Compensate.ICompensateStorage<{nameof(TModel)} > repository.");

            // 调用补偿仓储获取
            var storage = (ICompensateCheckStorage<TModel>)this.ServiceProvider.GetRequiredService<ICompensateStorage<TModel>>();
            var dataList = await storage.GetWaitingSyncAsync(this._options.CompleteCheckOnceCount);
            if (dataList == null || dataList.Count() == 0)
                return int.MinValue;

            // 获取es是否已经同步
            var versions = await this.GetVersionListAsync(dataList.Select(f => f.Id));
            var waitSyncIds = dataList.Where(f =>
            {
                if (versions.ContainsKey(f.Id))
                {
                    // 版本相同情况下无需补偿
                    if (versions[f.Id] >= f.Version)
                    {
                        this._syncedMarkProcessor.MarkSynced(f.Id);
                        return false;
                    }
                }
                return true;
            }).Select(f => f.Id).ToList();

            // 获取所有待补偿的数据
            if (waitSyncIds.Count > 0)
            {
                var models = await storage.GetListAsync(waitSyncIds);
                await this.IndexManyAsync(models);
            }
            // 等待全部标记完成
            await this._syncedMarkProcessor.WaitMarkComplete();
            return waitSyncIds.Count();

        }

        public virtual Task<IDictionary<string, long>> GetVersionListAsync(IEnumerable<string> ids)
        {
            return this._client.GetVersionListAsync(ids);
        }

    }
    public class ElasticsearchStorage
    {
        /// <summary>
        /// 默认 Elasticsearch 存储
        /// </summary>
        public const string DefaultName = "ElasticsearchStorage";

    }
}
