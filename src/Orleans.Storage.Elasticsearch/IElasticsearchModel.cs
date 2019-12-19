namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch存储模型
    /// </summary>
    public interface IElasticsearchModel
    {
        /// <summary>
        /// 获取唯一标识
        /// </summary>
        /// <returns></returns>
        public string GetPrimaryKey();
    }
}
