using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public class ElasticsearchClient<TDocument> : IElasticsearchClient<TDocument>
        where TDocument : class
    {
        private readonly IElasticClient _client;
        private readonly IndexName _indexName;
        private readonly TypeName _typeName;
        private readonly ILogger _logger;

        public ElasticsearchClient(IServiceProvider serviceProvider, IElasticClient client, IndexName indexName, TypeName typeName)
        {
            _client = client;
            _indexName = indexName;
            _typeName = typeName;
            _logger = serviceProvider.GetRequiredService<ILogger<ElasticsearchClient<TDocument>>>();
        }
        public async Task<bool> DeleteAsync(string id)
        {
            var response = await this._client.DeleteAsync(new DeleteRequest(this._indexName, this._typeName, id));
            this.Loggin(response);
            if (!response.IsValid)
            {
                return false;
            }
            else
                return true;
        }

        public async Task<IBulkResponse> DeleteManyAsync(IEnumerable<string> ids)
        {
            var response = await this._client.BulkAsync(b => b.DeleteMany<string>(ids).Index(this._indexName).Type(this._typeName));
            this.Loggin(response);
            return response;
        }

        public async Task<TDocument> GetAsync(string id)
        {
            var response = await this._client.GetAsync<TDocument>(new GetRequest(_indexName, _typeName, id));
            this.Loggin(response);
            if (!response.IsValid)
                return null;
            return response.Source;
        }

        public async Task<IEnumerable<TDocument>> GetListAsync(IEnumerable<string> ids)
        {
            var responses = await this._client.GetManyAsync<TDocument>(ids, _indexName, _typeName);
         
            return responses?.ToList().Select(f => f.Source);
        }

        public async Task<IDictionary<string, long>> GetVersionListAsync(IEnumerable<string> ids)
        {
            var response = await this._client.MultiGetAsync(r => r.Index(_indexName).Type(_typeName).GetMany<string>(ids).SourceEnabled(false));
            if (!response.IsValid)
                return new Dictionary<string, long>();
            else
                return response.Hits.ToDictionary(f => f.Id, v => (long)v.Version);
        }

        public async Task<IIndexResponse> IndexAsync(ElasticsearchDocument<TDocument> document)
        {
            var response = await this._client.IndexAsync(document.Document, idx =>
            {
                var indexDescriptor = idx.Index(this._indexName).Type(this._typeName).Id(document.PrimaryKey);
                if (document.VersionNo != int.MinValue && document.VersionNo > 0)
                    indexDescriptor.Version(document.VersionNo).VersionType(document.VersionType);
                return indexDescriptor;
            });
            this.Loggin(response);
            return response;
        }
        public async Task<IBulkResponse> IndexManyAsync(IEnumerable<ElasticsearchDocument<TDocument>> documents)
        {
            var response = await this._client.BulkAsync(b =>
            {
                var bulkDescriptor = b.Index(this._indexName).Type(this._typeName);
                bulkDescriptor.IndexMany(documents.Select(f => f.Document), (des, doc) =>
                {
                    var document = documents.FirstOrDefault(f => f.Document == doc);
                    des.Id(document.PrimaryKey);
                    if (document.VersionNo != int.MinValue && document.VersionNo > 0)
                        des.Version(document.VersionNo).VersionType(document.VersionType);
                    return des;
                });
                return bulkDescriptor;
            });
            this.Loggin(response);
            return response;
        }
        public void Loggin(IResponse response)
        {
            if (!response.IsValid)
            {
                if (response.TryGetServerErrorReason(out var reason))
                {
                    this._logger.LogWarning($"request elasticsearch filed ; reason : {reason}");
                }
                else if (response.OriginalException != null)
                {
                    this._logger.LogWarning(response.OriginalException, "request elasticsearch filed");
                }
                else
                {
                    this._logger.LogWarning($"request elasticsearch filed ");
                }
            }
        }
    }
}
