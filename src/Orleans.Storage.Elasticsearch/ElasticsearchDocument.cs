using Elasticsearch.Net;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch 文档
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class ElasticsearchDocument<TDocument>
    {
        /// <summary>
        /// Elasticsearch 存储文档
        /// </summary>
        /// <param name="document">文档数据</param>
        /// <param name="primaryKey">唯一标识</param>
        /// <param name="versionNo">版本号</param>
        public ElasticsearchDocument(TDocument document, string primaryKey, int versionNo)
        {
            Document = document;
            VersionNo = versionNo;
            PrimaryKey = primaryKey;
        }
        /// <summary>
        /// Elasticsearch 存储文档
        /// </summary>
        /// <param name="document">文档数据</param>
        /// <param name="primaryKey">唯一标识</param>
        public ElasticsearchDocument(TDocument document, string primaryKey)
        {
            Document = document;
            PrimaryKey = primaryKey;
        }
        /// <summary>
        /// 存储文档
        /// </summary>
        public TDocument Document { get; }
        /// <summary>
        /// 版本号
        /// </summary>
        public int VersionNo { get; } = -1;
        /// <summary>
        /// Elasticsearch 文档版本号类型
        /// </summary>
        public VersionType VersionType { get; set; } = VersionType.External;
        /// <summary>
        /// 唯一标识号
        /// </summary>
        public string PrimaryKey { get; }

    }
}
