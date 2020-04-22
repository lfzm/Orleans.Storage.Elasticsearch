using Nest;
using Orleans.Storage.Elasticsearch.Compensate;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Test.Mapping
{
    public class UserCompensateStorage : ICompensateStorage<UserModel>
    {
        public void Dispose()
        {
            
        }

        public Task<UserModel> GetAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserModel>> GetListAsync(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CompensateData>> GetWaitingSyncAsync(int size)
        {
            throw new NotImplementedException();
        }

        public Task ModifySyncedStatus(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }
    }
}
