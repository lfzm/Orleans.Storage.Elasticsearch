using Nest;
using Orleans.Storage.Elasticsearch.Compensate;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Test.Mapping
{
    public class CompensateStorage : ElasticsearchCompleteCheckStorage<UserModel, UserDocument>
    {
        public CompensateStorage(IServiceProvider serviceProvider):base(serviceProvider)
        {

        }
        public override Task<IDeleteResponse> DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }

        public override Task<List<CompensateData>> GetUnsyncListAsync(int count)
        {
            throw new NotImplementedException();
        }

        public override Task<IIndexResponse> IndexAsync(UserModel entity)
        {
            throw new NotImplementedException();
        }

        public override Task MarkSyncedAsync(List<string> primaryKeys)
        {
            throw new NotImplementedException();
        }

        public override Task<UserModel> QueryAsync(string id)
        {
            throw new NotImplementedException();
        }

        public override Task<UserModel> ReadNewestStateAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}
