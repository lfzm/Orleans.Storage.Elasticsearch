using Nest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch 存储器
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IElasticsearchStorage<TModel> : IElasticsearchStorage
        where TModel : IStorageModel
    {
        new Task<TModel> GetAsync(string id);
        new Task<IEnumerable<TModel>> GetListAsync(IEnumerable<string> ids);
        Task<bool> IndexAsync(TModel data);
        Task IndexManyAsync(IEnumerable<TModel> modelList);

    }
    /// <summary>
    /// Elasticsearch 存储器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IElasticsearchStorage
    {
        Task<object> GetAsync(string id);
        Task<IEnumerable<object>> GetListAsync(IEnumerable<string> ids);
        Task<bool> IndexAsync(object model);
        Task IndexManyAsync(IEnumerable<object> modelList);
        Task DeleteManyAsync(IEnumerable<string> ids);
        Task<bool> DeleteAsync(string id);

        internal Task<bool> RefreshAsync(string id);
        internal Task<int> CompensateSync();
        internal Task<object> GetToDbAsync(string Id);
    }
}
