using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Storage.Elasticsearch
{
    public interface IElasticsearchStorageBuilder
    {
        IServiceCollection Service { get; }
        IElasticsearchStorageBuilder Configure(Action<ConnectionSettings> action);
        IElasticsearchStorageBuilder Configure(ConnectionSettings settings);

        IElasticsearchStorageBuilder AddStorage<TStorage, TEntity>()
            where TStorage : class, IElasticsearchStorage
            where TEntity : class;

        /// <summary>
        /// 添加 Elasticsearch 存储 （自动创建 Index）
        /// </summary>
        /// <typeparam name="TStorage"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="indexName"></param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddStorage<TStorage, TEntity, TModel>(string indexName)
           where TStorage : class, IElasticsearchStorage
           where TEntity : class
           where TModel : class;
   
        void Build();
    }
}
