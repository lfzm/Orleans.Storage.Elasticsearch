using System;
using System.Collections.Generic;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch
    /// </summary>
    public class ElasticsearchStorageOptions
    {
        public ElasticsearchStorageOptions()
        {
            this.CompleteCheckOnceCount = 1000;
            this.CompleteCheckTimeOut = TimeSpan.FromMinutes(30);
            this.MarkWaitInterval = TimeSpan.FromMinutes(10);
            this.MarkProcessMaxCount = 200;
            this.IndexManyMaxCount = 2500;
        }
        /// <summary>
        /// 完整性检测的Index
        /// </summary>
        public List<string> CompleteCheckIndexList { get; internal set; } = new List<string>();
        /// <summary>
        /// 完整检查一次检查数量
        /// </summary>
        public int CompleteCheckOnceCount { get; set; }
        /// <summary>
        /// 完整检查超时时间(检查会出现死循环)
        /// </summary>
        public TimeSpan CompleteCheckTimeOut { get; set; }
        /// <summary>
        /// 标记已同步处理间隔
        /// </summary>
        public TimeSpan MarkWaitInterval { get; set; }
        /// <summary>
        /// 标记已同步最大处理数量
        /// </summary>
        public int MarkProcessMaxCount { get; set; }
        /// <summary>
        /// 批量写入es最大数量
        /// </summary>
        public int IndexManyMaxCount { get; set; }
    }
}
