using Microsoft.Extensions.DependencyInjection;
using Nest;
using Orleans.Runtime;
using Orleans.Storage.Elasticsearch.Compensate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Orleans.Storage.Elasticsearch
{
    public class ElasticsearchStorageBuilder : IElasticsearchStorageBuilder
    {
        /// <summary>
        /// 存储名称
        /// </summary>
        public string StorageName { get; }
        public IServiceCollection Services { get; }
        private readonly ElasticsearchIndexCreator _creator;
        private TimeSpan CompleteCheckInterval = TimeSpan.FromDays(1);
        private DateTime CompleteCheckStartTime = DateTime.Parse(DateTime.Now.ToString("T")).AddDays(1); // 默认晚上凌晨开始完整性检查
        private Action<ElasticsearchStorageOptions> StorageOptionsAction = opt => { };
        private List<ElasticsearchStorageInfo> StorageInfoList = new List<ElasticsearchStorageInfo>();//Elasticsearch 存储信息集合

        /// <summary>
        /// Elasticsearch 连接配置
        /// </summary>
        internal ConnectionSettings Settings = new ConnectionSettings();
        public ElasticsearchStorageBuilder(IServiceCollection service, string storageName)
        {
            this.Services = service;
            this.StorageName = storageName;
            this._creator = new ElasticsearchIndexCreator(this);
        }

        /// <summary>
        /// 配置 ElasticClient
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IElasticsearchStorageBuilder ConfigureConnection(Action<ConnectionSettings> action)
        {
            action.Invoke(this.Settings);
            return this;
        }
        /// <summary>
        /// 配置 ElasticClient
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IElasticsearchStorageBuilder ConfigureConnection(ConnectionSettings settings)
        {
            this.Settings = settings;
            return this;
        }
        /// <summary>
        /// 配置 ElasticsearchStorage
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IElasticsearchStorageBuilder Configure(Action<ElasticsearchStorageOptions> action)
        {
            this.StorageOptionsAction = action;
            return this;
        }


        /// <summary>
        /// 启用数据完整检查
        /// </summary>
        /// <param name="checkStartTime">检查开始时间</param>
        /// <param name="checkInterval">检查间隔</param>
        /// <returns></returns>
        public IElasticsearchStorageBuilder ConfigureCompleteCheck(DateTime checkStartTime, TimeSpan checkInterval)
        {
            this.CompleteCheckInterval = checkInterval;
            this.CompleteCheckStartTime = checkStartTime;
            return this;
        }

        public IElasticsearchStorageBuilder AddDocumentConverter<TConverter>()
            where TConverter : class, IStorageDocumentConverter
        {
            this.Services.AddSingleton<IStorageDocumentConverter, TConverter>();
            return this;
        }
        public IElasticsearchStorageBuilder AddStorage<TModel>(string indexName)
            where TModel : class, IStorageModel
        {
            this.StorageInfoList.Add(new ElasticsearchStorageInfo(indexName, typeof(TModel), typeof(TModel)));
            this.Services.AddSingletonNamedService<IElasticsearchStorage<TModel>>(indexName, (sp, key) =>
            {
                var client = sp.GetRequiredService<IElasticsearchClient<TModel>>();
                return new ElasticsearchStorage<TModel>(sp, key, client);
            });
            this.Services.AddSingletonNamedService<IElasticsearchClient<TModel>>(indexName, (sp, key) =>
            {
                var client = sp.GetRequiredServiceByName<IElasticClient>(this.StorageName);
                var typeName = this.GetIndexTypeName(typeof(TModel));
                return new ElasticsearchClient<TModel>(sp, client, indexName, typeName);
            });
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName)
            where TModel : class, IStorageConcurrencyModel
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddStorage<TModel>(indexName);
            this.AddCompensateStorage<TStorage, TModel>(indexName);
            this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName).CompleteCheck = true;
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName, DateTime checkStartTime, TimeSpan checkInterval)
            where TModel : class, IStorageConcurrencyModel
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddStorage<TModel, TStorage>(indexName);
            var info = this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName);
            info.CompleteCheck = true;
            info.CheckStartTime = checkStartTime;
            info.CheckInterval = checkInterval;
            return this;
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument>(string indexName)
            where TModel : class, IStorageModel
            where TDocument : class
        {
            this.StorageInfoList.Add(new ElasticsearchStorageInfo(indexName, typeof(TDocument), typeof(TModel)));
            this.Services.AddSingletonNamedService<IElasticsearchStorage<TModel>>(indexName, (sp, key) =>
            {
                var client = sp.GetRequiredService<IElasticsearchClient<TDocument>>();
                return new ElasticsearchStorage<TModel, TDocument>(sp, key, client);
            });
            this.Services.AddSingletonNamedService<IElasticsearchClient<TDocument>>(indexName, (sp, key) =>
            {
                var client = sp.GetRequiredServiceByName<IElasticClient>(this.StorageName);
                var typeName = this.GetIndexTypeName(typeof(TModel));
                return new ElasticsearchClient<TDocument>(sp, client, indexName, typeName);
            });
            return this;
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName)
            where TModel : class, IStorageConcurrencyModel
            where TDocument : class
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddMapperStorage<TModel, TDocument>(indexName);
            this.AddCompensateStorage<TStorage, TModel>(indexName);
            this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName).CompleteCheck = true;
            return this;
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName, DateTime checkStartTime, TimeSpan checkInterval)
            where TModel : class, IStorageConcurrencyModel
            where TDocument : class
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddMapperStorage<TModel, TDocument, TStorage>(indexName);
            var info = this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName);
            info.CompleteCheck = true;
            info.CheckStartTime = checkStartTime;
            info.CheckInterval = checkInterval;
            return this;
        }

        private IElasticsearchStorageBuilder AddCompensateStorage<TStorage, TModel>(string indexName)
            where TModel : class, IStorageConcurrencyModel
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.Services.AddSingletonNamedService<ISyncedStatusMarkProcessor>(indexName, (sp, key) => new SyncedStatusMarkProcessor(sp, key));
            this.Services.AddTransient<ICompensateStorage<TModel>, TStorage>();
            return this;
        }

        private string GetIndexTypeName(Type type)
        {
            var attr = type.GetCustomAttribute<ElasticsearchTypeAttribute>(true);
            if (attr == null)
                throw new ArgumentNullException("Please identify ElasticsearchTypeAttribute in Mapping Model");
            return attr.Name;
        }

        public void Build()
        {
            this.Services.Configure<ElasticsearchStorageOptions>(this.StorageName, opt =>
            {
                StorageOptionsAction?.Invoke(opt);
                opt.CompleteCheckIndexList = StorageInfoList.Where(f => f.CompleteCheck).Select(f => f.IndexName).ToList();
            });
            // 注入 ElasticClient
            this.Services.AddSingletonNamedService<IElasticClient>(this.StorageName, (sp, key) =>
            {
                var client = new ElasticClient(Settings);
                return client;
            });
            // 注入存储配置
            this.StorageInfoList.ForEach(f =>
            {
                this.Services.Configure<ElasticsearchStorageInfo>(f.IndexName, opt =>
                {
                    if (f.CompleteCheck)
                    {
                        if (opt.CheckInterval == null) opt.CheckInterval = this.CompleteCheckInterval;
                        if (opt.CheckStartTime == null) opt.CheckStartTime = this.CompleteCheckStartTime;
                        opt.CompleteCheck = true;
                    }
                    opt.StorageName = this.StorageName;
                    opt.IndexName = f.IndexName;
                    opt.DocumentType = f.DocumentType;
                });
            });
        }
    }
}
