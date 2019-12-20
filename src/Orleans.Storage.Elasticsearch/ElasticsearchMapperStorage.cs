using Microsoft.Extensions.DependencyInjection;
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
    /// Elasticsearch Storage (需要使用 <see cref="IStorageDocumentConverter"/> 把TModel转换TDocument)
    /// </summary>
    /// <typeparam name="TModel">数据对象</typeparam>
    /// <typeparam name="TDocument">Elasticsearch 存储对象（有标记Mapping映射）</typeparam>
    public class ElasticsearchStorage<TModel, TDocument> : ElasticsearchStorage<TModel>, IElasticsearchStorage<TModel>
        where TModel : class, IElasticsearchModel
        where TDocument : class
    {
        private readonly IStorageDocumentConverter _documentConverter;
        private readonly IElasticsearchClient<TDocument> _client;
        private readonly IDataflowBufferBlock<ElasticsearchDocument<TDocument>> _dataflowBuffer;
        public ElasticsearchStorage(IServiceProvider serviceProvider, string indexName, IElasticsearchClient<TDocument> client) : base(serviceProvider, indexName)
        {
            this._client = client;
            this._documentConverter = this.ServiceProvider.GetRequiredService<IStorageDocumentConverter>();
            this._dataflowBuffer = new DataflowBufferBlock<ElasticsearchDocument<TDocument>>(this.IndexMany);
        }

        public override async Task<TModel> GetAsync(string id)
        {
            var document = await _client.GetAsync(id);
            return this._documentConverter.ToModel<TModel, TDocument>(document);
        }
        public override async Task<IEnumerable<TModel>> GetListAsync(IEnumerable<string> ids)
        {
            var documents = await _client.GetListAsync(ids);
            return this._documentConverter.ToModelList<TModel, TDocument>(documents);
        }
        public override async Task<bool> IndexAsync(TModel model)
        {
            var doc = this._documentConverter.ToDocument<TDocument, TModel>(model);
            ElasticsearchDocument<TDocument> ed = null;
            if (model is IElasticsearchConcurrencyModel m)
                ed = new ElasticsearchDocument<TDocument>(doc, model.GetPrimaryKey(), m.GetVersionNo());
            else
                ed = new ElasticsearchDocument<TDocument>(doc, model.GetPrimaryKey());
            if (await this._dataflowBuffer.SendAsync(ed))
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
        public override async Task<bool> DeleteAsync(string id)
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
        public async Task IndexMany(BufferBlock<IDataflowBufferWrap<ElasticsearchDocument<TDocument>>> bufferBlock)
        {
            List<IDataflowBufferWrap<ElasticsearchDocument<TDocument>>> modelWraps = new List<IDataflowBufferWrap<ElasticsearchDocument<TDocument>>>();
            while (bufferBlock.TryReceive(out var wrap))
            {
                // 一次操作数据不能超过配置限制
                if (modelWraps.Count > this._options.IndexManyMaxCount)
                    break;
                modelWraps.Add(wrap);
            }
            var docs = modelWraps.Select(f => f.Data);
            var response = await this._client.IndexManyAsync(docs);
            if (response.IsValid)
                modelWraps.ForEach(f => f.CompleteHandler(true));// 执行成功全部返回成功
            else
            {
                // 部分执行失败，检查是否成功并且返回
                modelWraps.ForEach(w =>
                {
                    string id = w.Data.PrimaryKey;
                    var r = response.Items.FirstOrDefault(f => f.Id == id);
                    if (!r.IsValid)
                    {
                        // 如果有版本冲突，默认成功执行
                        if (r.Status == 409 && r.Error.Reason.Contains("version conflict"))
                        {
                            w.CompleteHandler(true);
                            return;
                        }
                    }
                    w.CompleteHandler(r.IsValid);
                });
            }
        }

        public override Task<IDictionary<string, long>> GetVersionListAsync(IEnumerable<string> ids)
        {
            return this._client.GetVersionListAsync(ids);
        }
    }
}
