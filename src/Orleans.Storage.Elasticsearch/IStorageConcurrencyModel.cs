namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// /// <summary>
    /// Elasticsearch存储并发模型
    /// </summary>
    /// </summary>
    public interface IStorageConcurrencyModel : IStorageModel
    {
        public int GetVersionNo();
    }
}
