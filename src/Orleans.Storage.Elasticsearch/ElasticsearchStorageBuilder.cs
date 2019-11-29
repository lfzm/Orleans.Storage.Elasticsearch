using Microsoft.Extensions.DependencyInjection;
using Nest;
using Orleans.Runtime;
using System;

namespace Orleans.Storage.Elasticsearch
{
    public class ElasticsearchStorageBuilder : IElasticsearchStorageBuilder
    {
        /// <summary>
        /// 存储名称
        /// </summary>
        public string StorageName { get; }
        public IServiceCollection Service { get; }
        private readonly ElasticsearchIndexCreator creator;

        /// <summary>
        /// Elasticsearch 连接配置
        /// </summary>
        internal ConnectionSettings Settings = new ConnectionSettings();
        public ElasticsearchStorageBuilder(IServiceCollection service, string storageName)
        {
            this.Service = service;
            this.StorageName = storageName;
            this.creator = new ElasticsearchIndexCreator(this);
        }

        /// <summary>
        /// 配置 ElasticClient
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IElasticsearchStorageBuilder Configure(Action<ConnectionSettings> action)
        {
            action.Invoke(this.Settings);
            return this;
        }

        /// <summary>
        /// 配置 ElasticClient
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IElasticsearchStorageBuilder Configure(ConnectionSettings settings)
        {
            this.Settings = settings;
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TStorage, TEntity>()
            where TStorage : class, IElasticsearchStorage
            where TEntity : class
        {
            string name = typeof(TEntity).FullName;
            Service.AddTransient<TStorage>();
            Service.AddTransientNamedService<IElasticsearchStorage>(name, (sp, key) =>
            {
                var storage = sp.GetRequiredService<TStorage>();
                storage.Client = sp.GetRequiredServiceByName<IElasticClient>(this.StorageName);
                return storage;
            });
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TStorage, TEntity, TModel>(string indexName)
           where TStorage : class, IElasticsearchStorage
           where TEntity : class
           where TModel : class
        {
            this.AddStorage<TStorage, TEntity>();
            this.creator.Add<TModel>(indexName);
            return this;
        }

        public void Build()
        {
            this.Service.AddSingleton<ElasticsearchResponseFailedHandle>();
            this.Service.AddSingletonNamedService<IElasticClient>(this.StorageName, (sp, key) =>
            {
                var client = new ElasticClient(Settings);
                return client;
            });

            //创建所有索引
            this.creator.Create();
        }
    }
}
