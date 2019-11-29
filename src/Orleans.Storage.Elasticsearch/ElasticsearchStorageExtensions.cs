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

        /// <summary>
        /// 根据实体获取存储
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="type">实体类型</param>
        /// <returns></returns>
        public static IElasticsearchStorage GetElasticsearchStorage(this IServiceProvider serviceProvider, Type type) 
        {
            var name = type.FullName;
            return serviceProvider.GetRequiredServiceByName<IElasticsearchStorage>(name);
        }
    }
}
