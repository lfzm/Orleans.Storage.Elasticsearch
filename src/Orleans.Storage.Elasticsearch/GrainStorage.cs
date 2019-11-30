using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public class GrainStorage : IGrainStorage
    {
        private IServiceProvider ServiceProvider;
        public GrainStorage(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var id = this.GetPrimaryKeyObject(grainReference).ToString();
            await this.GetRepository(grainState).ClearAsync(id);
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var id = this.GetPrimaryKeyObject(grainReference).ToString();
            grainState.State = await this.GetRepository(grainState).ReadAsync(id);
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var id = this.GetPrimaryKeyObject(grainReference).ToString();
            await this.GetRepository(grainState).WriteAsync(id,grainState.State);
        }

        private IElasticsearchStorage GetRepository(IGrainState grainState)
        {
            var repositoryStorage = ServiceProvider.GetServiceByName<IElasticsearchStorage>(grainState.State.GetType().FullName);
            if (repositoryStorage == null)
                throw new ArgumentNullException(string.Format("{0} State Repository Unrealized", grainState.State.GetType().Name));
            else
                return repositoryStorage;
        }

        /// <summary>
        /// Get Grain PrimaryKey
        /// </summary>
        /// <param name="grainReference"></param>
        /// <returns></returns>
        public object GetPrimaryKeyObject(IAddressable addressable)
        {
            var key = addressable.GetPrimaryKeyString();
            if (key != null)
                return key;
            if (addressable.IsPrimaryKeyBasedOnLong())
                return addressable.GetPrimaryKeyLong();
            else
                return addressable.GetPrimaryKey();
        }
    }
}
