using Nest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch 客户端
    /// </summary>
    public interface IElasticsearchClient<TDocument>: IElasticsearchClient
    {
        Task<TDocument> GetAsync(string id);
        Task<IEnumerable<TDocument>> GetListAsync(IEnumerable<string> ids);
        Task<IIndexResponse> IndexAsync(ElasticsearchDocument<TDocument> document);
        Task<IBulkResponse> IndexManyAsync(IEnumerable<ElasticsearchDocument<TDocument>> documents);
    }

    public interface IElasticsearchClient
    {
        Task<IDictionary<string, long>> GetVersionListAsync(IEnumerable<string> ids);
        Task<bool> DeleteAsync(string id);
        Task<IBulkResponse> DeleteManyAsync(IEnumerable<string> ids);
    }
}
