using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    /// <summary>
    /// 同步到 Elasticsearch 状态标记处理器
    /// multi producter single consumer channel
    /// </summary>
    public interface ISyncedStatusMarkProcessor
    {
        /// <summary>
        /// 标记已同步
        /// </summary>
        /// <param name="id">唯一标识</param>
        void MarkSynced(string id);

        /// <summary>
        /// 等待标记完成
        /// </summary>
        /// <returns></returns>
        Task WaitMarkComplete();
    }
}
