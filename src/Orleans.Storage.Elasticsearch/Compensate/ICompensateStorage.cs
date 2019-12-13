using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public interface ICompensateStorage<TModel> : ICompensateStorage
        where TModel : IStorageModel
    {
        Task<TModel> GetAsync(string id);

        Task<IEnumerable<TModel>> GetListAsync(IEnumerable<string> ids);
    }

    public interface ICompensateStorage
    {
        Task ModifySyncedStatus(IEnumerable<string> ids);

        Task<IEnumerable<CompensateData>> GetWaitingSyncAsync(int size);
    }
}
