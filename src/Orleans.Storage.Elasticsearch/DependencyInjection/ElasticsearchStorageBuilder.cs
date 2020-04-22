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
        private ConnectionSettings Settings { get; set; } = new ConnectionSettings();

        private TimeSpan CompleteCheckInterval = TimeSpan.FromDays(1);
        private DateTime CompleteCheckStartTime = DateTime.Parse(DateTime.Now.ToString("d")).AddDays(1); // 默认晚上凌晨开始完整性检查
        private Action<ElasticsearchStorageOptions> StorageOptionsAction = opt => { };
        private List<ElasticsearchStorageInfo> StorageInfoList = new List<ElasticsearchStorageInfo>();//Elasticsearch 存储信息集合
        public ElasticsearchStorageBuilder(IServiceCollection service, string storageName)
        {
            this.Services = service;
            this.StorageName = storageName;
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
            where TModel : class, IElasticsearchModel
        {
            this.StorageInfoList.Add(new ElasticsearchStorageInfo(indexName, typeof(TModel), typeof(TModel)));
            this.Services.AddSingleton<IElasticsearchStorage<TModel>>((sp) =>
           {
               var client = sp.GetRequiredService<IElasticsearchClient<TModel>>();
               return new ElasticsearchStorage<TModel>(sp, indexName, client);
           });
            this.Services.AddSingleton<IElasticsearchClient<TModel>>((sp) =>
           {
               var client = sp.GetRequiredServiceByName<IElasticClient>(this.StorageName);
               var typeName = this.GetIndexTypeName(typeof(TModel));
               return new ElasticsearchClient<TModel>(sp, client, indexName, typeName);
           });
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName)
            where TModel : class, IElasticsearchModel
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddStorage<TModel>(indexName);
            this.AddCompensateStorage<TStorage, TModel>(indexName);
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName, DateTime checkStartTime, TimeSpan checkInterval)
            where TModel : class, IElasticsearchModel
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddStorage<TModel, TStorage>(indexName);
            var info = this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName);
            info.CheckStartTime = checkStartTime;
            info.CheckInterval = checkInterval;
            return this;
        }

        public IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName, TimeSpan checkInterval)
        where TModel : class, IElasticsearchModel
        where TStorage : class, ICompensateStorage<TModel>
        {
            return this.AddStorage<TModel, TStorage>(indexName, DateTime.Now, checkInterval);
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument>(string indexName)
            where TModel : class, IElasticsearchModel
            where TDocument : class
        {
            this.StorageInfoList.Add(new ElasticsearchStorageInfo(indexName, typeof(TDocument), typeof(TModel)));
            this.Services.AddTransient<IElasticsearchStorage<TModel>>((sp) =>
            {
                var client = sp.GetRequiredService<IElasticsearchClient<TDocument>>();
                return new ElasticsearchStorage<TModel, TDocument>(sp, indexName, client);
            });
            this.Services.AddSingleton<IElasticsearchClient<TDocument>>((sp) =>
            {
                var client = sp.GetRequiredServiceByName<IElasticClient>(this.StorageName);
                var typeName = this.GetIndexTypeName(typeof(TDocument));
                return new ElasticsearchClient<TDocument>(sp, client, indexName, typeName);
            });
            return this;
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName)
            where TModel : class, IElasticsearchModel
            where TDocument : class
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddMapperStorage<TModel, TDocument>(indexName);
            this.AddCompensateStorage<TStorage, TModel>(indexName);
            return this;
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName, TimeSpan checkInterval)
           where TModel : class, IElasticsearchModel
           where TDocument : class
           where TStorage : class, ICompensateStorage<TModel>
        {
            return this.AddMapperStorage<TModel, TDocument, TStorage>(indexName, DateTime.Now, checkInterval);
        }

        public IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName, DateTime checkStartTime, TimeSpan checkInterval)
            where TModel : class, IElasticsearchModel
            where TDocument : class
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.AddMapperStorage<TModel, TDocument, TStorage>(indexName);
            var info = this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName);
            info.CheckStartTime = checkStartTime;
            info.CheckInterval = checkInterval;
            return this;
        }

        private IElasticsearchStorageBuilder AddCompensateStorage<TStorage, TModel>(string indexName)
            where TModel : class, IElasticsearchModel
            where TStorage : class, ICompensateStorage<TModel>
        {
            this.Services.AddTransient<ICompensateStorage<TModel>, TStorage>();
            var info = this.StorageInfoList.FirstOrDefault(f => f.IndexName == indexName);
            info.Compensate = true;
            if (typeof(ICompensateCheckStorage<TModel>).IsAssignableFrom(typeof(TStorage)) 
                && typeof(IElasticsearchConcurrencyModel).IsAssignableFrom(typeof(TModel)))
            {
                // 如有继承ICompensateStorage<> 和 IElasticsearchConcurrencyModel 启动完整性检查
                this.Services.AddSingletonNamedService<ISyncedStatusMarkProcessor>(indexName, (sp, key) => new SyncedStatusMarkProcessor(sp, key));
                info.CompleteCheck = true;
            }
            return this;
        }

        private string GetIndexTypeName(Type type)
        {
            var attr = type.GetCustomAttribute<ElasticsearchTypeAttribute>(true);
            if (attr == null)
                throw new ArgumentNullException($"{type.FullName} Please identify ElasticsearchTypeAttribute in Mapping Model");
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
                        opt.CheckInterval = f.CheckInterval.TotalMilliseconds == 0 ? this.CompleteCheckInterval : f.CheckInterval;
                        opt.CheckStartTime = f.CheckStartTime == DateTime.MinValue ? this.CompleteCheckStartTime : f.CheckStartTime;
                        opt.CompleteCheck = true;
                    }
                    opt.StorageName = this.StorageName;
                    opt.ModelType = f.ModelType;
                    opt.Compensate = f.Compensate;
                    opt.IndexName = f.IndexName;
                    opt.DocumentType = f.DocumentType;
                });
            });

            // 自动创建索引
            new ElasticsearchIndexCreator(this.Settings).Create(this.StorageInfoList);
        }
    }
}
