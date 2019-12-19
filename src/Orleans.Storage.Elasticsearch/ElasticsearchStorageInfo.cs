using Nest;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch storage info
    /// </summary>
    public class ElasticsearchStorageInfo
    {
        public ElasticsearchStorageInfo() { }
        public ElasticsearchStorageInfo(string indexName, Type documentType, Type modelType)
        {
            this.IndexName = indexName;
            this.DocumentType = documentType;
            this.ModelType = modelType;
        }
        /// <summary>
        /// 存储信息
        /// </summary>
        public string StorageName { get; set; }
        /// <summary>
        /// Elasticsearch 索引名称
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// 文档对象类型
        /// </summary>
        public Type DocumentType { get; set; }
        /// <summary>
        /// 文档对象类型
        /// </summary>
        public Type ModelType { get; set; }
        /// <summary>
        /// 自动完整性检查
        /// </summary>
        public bool CompleteCheck { get; set; }
        /// <summary>
        /// 是否启动补偿
        /// </summary>
        public bool Compensate { get; set; }
        /// <summary>
        /// 刷新间隔(单位分钟) 最少刷新时间1分钟
        /// </summary>
        public TimeSpan CheckInterval { get; set; }
        /// <summary>
        /// 检查时间
        /// </summary>
        public DateTime CheckStartTime { get; set; }
    }
}
