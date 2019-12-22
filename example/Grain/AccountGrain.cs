using Orleans.Providers;
using Orleans.Storage.Elasticsearch;
using System.Threading.Tasks;

namespace Grain
{
    [StorageProvider(ProviderName = ElasticsearchStorage.DefaultName)]
    public class AccountGrain : Orleans.Grain<AccountModel>, IAccountGrain
    {
        public async Task Add(AccountModel account)
        {
            this.State = account;
            await this.WriteStateAsync();
        }
    }
}
