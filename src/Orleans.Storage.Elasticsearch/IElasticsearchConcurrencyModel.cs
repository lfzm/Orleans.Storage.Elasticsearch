namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// /// <summary>
    /// Elasticsearch存储并发模型
    /// </summary>
    /// </summary>
    public interface IElasticsearchConcurrencyModel : IElasticsearchModel
    {
        public int GetVersionNo();
    }
}
