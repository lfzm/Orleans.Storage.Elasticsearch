using Nest;
using System;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public interface IElasticsearchStorage
    {
        /// <summary>
        /// ElasticSearch客户端
        /// </summary>
        IElasticClient Client { get; set; }
        /// <summary>
        /// 读取存储
        /// </summary>
        /// <param name="id">标识Id</param>
        /// <returns></returns>
        Task<object> ReadAsync(string id);
        /// <summary>
        /// 写入存储
        /// </summary>
        /// <param name="id">标识Id</param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> WriteAsync(string id,object obj);
        /// <summary>
        /// 刷新存储
        /// </summary>
        /// <param name="id">实体 id</param>
        /// <returns></returns>
        Task<bool> RefreshAsync(string id);
        /// <summary>
        /// 删除存储
        /// </summary>
        /// <param name="id">实体 id</param>
        /// <returns></returns>
        Task<bool> ClearAsync(string id);
    }
}
