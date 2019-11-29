using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public interface ICompensateGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 写入异步补偿
        /// </summary>
        /// <param name="id">唯一标识</param>
        /// <param name="type">补偿类型</param>
        /// <returns></returns>
        Task WriteAsync(string id, CompensateType type);

        /// <summary>
        /// 补偿
        /// </summary>
        /// <param name="ids">唯一标识集合</param>
        /// <param name="type">补偿类型</param>
        /// <returns></returns>
        Task<int> CompensateAsync(List<string> ids, CompensateType type);
    }
}
