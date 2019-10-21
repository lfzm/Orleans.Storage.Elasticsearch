using Orleans.Runtime;
using System;

namespace Orleans.Storage.Elasticsearch
{
    public static class ElasticsearchStorageExtensions
    {
        /// <summary>
        /// 根据实体获取存储
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static IElasticsearchStorage GetElasticsearchStorage<TEntity>(this IServiceProvider serviceProvider) where TEntity : class
        {
            var name = typeof(TEntity).FullName;
            return serviceProvider.GetRequiredServiceByName<IElasticsearchStorage>(name);
        }
    }
}
