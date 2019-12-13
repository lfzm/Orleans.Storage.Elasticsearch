using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public interface ICompensater : IGrainWithStringKey
    {
        /// <summary>
        /// 异步补偿
        /// </summary>
        /// <param name="id">补偿的数据</param>
        /// <returns></returns>
        Task CompensateAsync(CompensateData data);
        /// <summary>
        /// 完整性检查
        /// </summary>
        /// <returns></returns>
        Task CompletaCheckAsync();
     
    }
}
