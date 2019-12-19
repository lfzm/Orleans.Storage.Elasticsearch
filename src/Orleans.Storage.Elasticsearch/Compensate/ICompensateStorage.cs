using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public interface ICompensateCheckStorage<TModel> : ICompensateStorage<TModel>, ICompensateCheckStorage
        where TModel : IElasticsearchModel
    {
        Task<IEnumerable<TModel>> GetListAsync(IEnumerable<string> ids);
    }

    public interface ICompensateCheckStorage
    {
        Task ModifySyncedStatus(IEnumerable<string> ids);

        Task<IEnumerable<CompensateData>> GetWaitingSyncAsync(int size);
    }

    public interface ICompensateStorage<TModel>
          where TModel : IElasticsearchModel
    {
        Task<TModel> GetAsync(string id);
    }
}
