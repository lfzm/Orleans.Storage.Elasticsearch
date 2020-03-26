using Nest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch 存储器
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IElasticsearchStorage<TModel> : IElasticsearchStorage
        where TModel : IElasticsearchModel
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

        Task<bool> RefreshAsync(string id);
        Task<object> GetToDbAsync(string Id);
        /// <summary>
        /// Compensate Sync
        /// </summary>
        /// <returns>Returns whether processing has been completed</returns>
        Task<bool> CompensateSync();
    }
}
