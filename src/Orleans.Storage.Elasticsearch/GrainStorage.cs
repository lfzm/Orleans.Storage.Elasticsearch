using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public class GrainStorage : IGrainStorage
    {
        /// <summary>
        /// Grain Storage 前往数据库读取Model 类型
        /// </summary>
        private static Dictionary<string, bool> GrainReadByDBModel = new Dictionary<string, bool>();
        private readonly IServiceProvider ServiceProvider;

        public static void ConfigureToDBRead(params Type[] types)
        {
            foreach (var type in types)
            {
                if (!GrainReadByDBModel.ContainsKey(type.FullName))
                    GrainReadByDBModel.Add(type.FullName, true);
            }
        }
        public GrainStorage(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var id = this.GetPrimaryKeyObject(grainReference).ToString();
            await this.GetRepository(grainState).DeleteAsync(id);
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var id = this.GetPrimaryKeyObject(grainReference).ToString();
            //如果设置为数据库获取，就前往数据库进行获取数据
            if (GrainReadByDBModel.ContainsKey(grainState.Type.FullName))
                grainState.State = await this.GetRepository(grainState).GetToDbAsync(id);
            else
                grainState.State = await this.GetRepository(grainState).GetAsync(id);
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var id = this.GetPrimaryKeyObject(grainReference).ToString();
            await this.GetRepository(grainState).IndexAsync(grainState.State);
        }

        public IElasticsearchStorage GetRepository(IGrainState grainState)
        {
            var repositoryStorage = this.ServiceProvider.GetRequiredService(typeof(IElasticsearchStorage<>).MakeGenericType(grainState.Type));
            if (repositoryStorage == null)
                throw new ArgumentNullException(string.Format("{0} State Repository Unrealized", grainState.State.GetType().Name));
            else
                return (IElasticsearchStorage)repositoryStorage;
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
