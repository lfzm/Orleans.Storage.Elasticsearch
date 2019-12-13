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
            if (!response.IsValid)
            {
                return false;
            }
            else
                return true;
        }

        public Task<IBulkResponse> DeleteManyAsync(IEnumerable<string> ids)
        {
            return this._client.DeleteManyAsync(ids, this._indexName, this._typeName);
        }

        public async Task<TDocument> GetAsync(string id)
        {
            var response = await this._client.GetAsync<TDocument>(new GetRequest(_indexName, _typeName, id));
            if (response.IsValid)
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
            var response = await this._client.MultiGetAsync(r => r.Index(_indexName).Type(_typeName).SourceEnabled(false));
            if (response.IsValid)
                return new Dictionary<string, long>();
            else
                return response.Hits.ToDictionary(f => f.Id, v => (long)v.Version);
        }

        public async Task<IIndexResponse> IndexAsync(ElasticsearchDocument<TDocument> document)
        {
            return await this._client.IndexAsync(document.Document, idx =>
            {
                var indexDescriptor = idx.Index(this._indexName).Type(this._typeName).Id(document.PrimaryKey);
                if (document.VersionNo > -1)
                    indexDescriptor.Version(document.VersionNo).VersionType(document.VersionType);
                return indexDescriptor;
            });
        }

        public async Task<IBulkResponse> IndexManyAsync(IEnumerable<ElasticsearchDocument<TDocument>> documents)
        {
            return await this._client.BulkAsync(b =>
            {
                var bulkDescriptor = b.Index(this._indexName).Type(this._typeName);
                foreach (var document in documents)
                {
                    return bulkDescriptor.Index<TDocument>(x =>
                    {
                        x.Document(document.Document);
                        x.Id(document.PrimaryKey);
                        if (document.VersionNo > -1)
                            x.Version(document.VersionNo).VersionType(document.VersionType);
                        return x;
                    });
                }
                return bulkDescriptor;
            });
        }

        public void Handle(IResponse response)
        {
            if (response.TryGetServerErrorReason(out var reason))
            {
                this._logger.LogError($"request elasticsearch filed ; reason : {reason}");
            }
            else if (response.OriginalException != null)
            {
                this._logger.LogError(response.OriginalException, "request elasticsearch filed");
            }
            else
            {
                this._logger.LogError($"request elasticsearch filed ");
            }
        }
    }
}
