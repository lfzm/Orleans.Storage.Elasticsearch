using Microsoft.Extensions.DependencyInjection;
using Nest;
using Orleans.Storage.Elasticsearch.Compensate;
using System;

namespace Orleans.Storage.Elasticsearch
{
    public interface IElasticsearchStorageBuilder
    {
        IServiceCollection Services { get; }
        /// <summary>
        /// Setting ElasticsearchStorageOptions 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IElasticsearchStorageBuilder Configure(Action<ElasticsearchStorageOptions> action);
        /// <summary>
        /// Setting Elasticsearch connection string
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IElasticsearchStorageBuilder ConfigureConnection(Action<ConnectionSettings> action);
        /// <summary>
        /// Setting Elasticsearch connection string
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        IElasticsearchStorageBuilder ConfigureConnection(ConnectionSettings settings);
        /// <summary>
        /// 启用完整性检查
        /// </summary>
        /// <param name="checkStartTime">检查开始时间</param>
        /// <param name="checkInterval">检查时间间隔</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder ConfigureCompleteCheck(DateTime checkStartTime, TimeSpan checkInterval);
        /// <summary>
        /// 添加Elasticsearch 存储对象转换器
        /// </summary>
        /// <typeparam name="TConverter"></typeparam>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddDocumentConverter<TConverter>()
            where TConverter : class,IStorageDocumentConverter;
        /// <summary>
        /// 配置 Elasticsearch Storage
        /// 数据Model和 Elasticsearch mapping model 一样情况下使用
        /// </summary>
        /// <typeparam name="TModel">数据Model</typeparam>
        /// <param name="indexName">Elasticsearch 索引名称</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddStorage<TModel>(string indexName)
           where TModel :class, IStorageModel;
        /// <summary>
        /// 配置 Elasticsearch Storage
        /// 数据Model和 Elasticsearch mapping model 一样情况下使用
        /// </summary>
        /// <typeparam name="TModel">数据 Model</typeparam>
        /// <typeparam name="TStorage">完整性检查存储</typeparam>
        /// <param name="indexName">Elasticsearch 索引名称</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName)
           where TModel : class, IStorageConcurrencyModel
           where TStorage : class, ICompensateStorage<TModel>;
        /// <summary>
        /// 配置 Elasticsearch Storage
        /// 数据Model和 Elasticsearch mapping model 一样情况下使用
        /// </summary>
        /// <typeparam name="TModel">数据 Model</typeparam>
        /// <typeparam name="TStorage">完整性检查存储</typeparam>
        /// <param name="indexName">Elasticsearch 索引名称</param>
        /// <param name="checkStartTime">检查开始时间</param>
        /// <param name="checkInterval">检查时间间隔</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddStorage<TModel, TStorage>(string indexName, DateTime checkStartTime, TimeSpan checkInterval)
           where TModel : class, IStorageConcurrencyModel
           where TStorage : class, ICompensateStorage<TModel>;
        /// <summary>
        /// 配置 Elasticsearch Storage
        /// 需要通过 <see cref="AddDocumentConverter"/> 方法配置Document转换器
        /// </summary>
        /// <typeparam name="TModel">数据Model</typeparam>
        /// <typeparam name="TDocument">Elasticsearch mapping model </typeparam>
        /// <param name="indexName">Elasticsearch 索引名称</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument>(string indexName)
           where TModel : class, IStorageModel
           where TDocument : class;
        /// <summary>
        /// 配置 Elasticsearch Storage
        /// 需要通过 <see cref="AddDocumentConverter"/> 方法配置Document转换器
        /// </summary>
        /// <typeparam name="TModel">数据Model</typeparam>
        /// <typeparam name="TDocument">Elasticsearch mapping model </typeparam>
        /// <typeparam name="TStorage">完整性检查存储</typeparam>
        /// <param name="indexName">Elasticsearch 索引名称</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName)
           where TModel : class, IStorageConcurrencyModel
           where TDocument : class
           where TStorage : class, ICompensateStorage<TModel>;

        /// <summary>
        /// 配置 Elasticsearch Storage
        /// 需要通过 <see cref="AddDocumentConverter"/> 方法配置Document转换器
        /// </summary>
        /// <typeparam name="TModel">数据Model</typeparam>
        /// <typeparam name="TDocument">Elasticsearch mapping model </typeparam>
        /// <typeparam name="TStorage">完整性检查存储</typeparam>
        /// <param name="indexName">Elasticsearch 索引名称</param>
        /// <param name="checkStartTime">检查开始时间</param>
        /// <param name="checkInterval">检查时间间隔</param>
        /// <returns></returns>
        IElasticsearchStorageBuilder AddMapperStorage<TModel, TDocument, TStorage>(string indexName, DateTime checkStartTime, TimeSpan checkInterval)
           where TModel : class, IStorageConcurrencyModel
           where TDocument : class
           where TStorage : class, ICompensateStorage<TModel>;

        /// <summary>
        /// 构建 Elasticsearch Storage
        /// </summary>
        void Build();
    }
}
