using Orleans.Providers;
using Orleans.Storage.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grain
{
    [StorageProvider(ProviderName = ElasticsearchStorage.DefaultName)]
    public class UserGrain : Orleans.Grain<UserModel>, IUserGrain
    {
        public Task AddUser(UserModel model)
        {
            this.State = model;
            return this.WriteStateAsync();
        }
    }
}
