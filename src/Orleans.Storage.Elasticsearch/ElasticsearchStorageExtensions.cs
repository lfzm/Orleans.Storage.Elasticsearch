using Orleans.Runtime;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public static class ElasticsearchStorageExtensions
    {
        /// <summary>
        /// 根据实体获取存储
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static IElasticsearchStorage<TModel> GetElasticsearchStorage<TModel>(this IServiceProvider serviceProvider)
             where TModel : IElasticsearchModel
        {
            return serviceProvider.GetRequiredService<IElasticsearchStorage<TModel>>();
        }

        /// <summary>
        /// 根据实体获取存储
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="type">实体类型</param>
        /// <returns></returns>
        public static IElasticsearchStorage GetElasticsearchStorage(this IServiceProvider serviceProvider, Type modelType)
        {
            if (!typeof(IElasticsearchModel).IsAssignableFrom(modelType))
                throw new Exception($"{modelType.FullName} needs to inherit Orleans.Storage.Elasticsearch.IElasticsearchModel");
            var type = typeof(IElasticsearchStorage<>).MakeGenericType(modelType);
            return (IElasticsearchStorage)serviceProvider.GetRequiredService(type);
        }

        public static async Task ElasticsearchIndexManyAsync(this IServiceProvider serviceProvider, params IElasticsearchModel[] models)
        {
            foreach (var m in models)
            {
                await serviceProvider.GetElasticsearchStorage(m.GetType()).IndexAsync(m);
            }
        }
    }
}
