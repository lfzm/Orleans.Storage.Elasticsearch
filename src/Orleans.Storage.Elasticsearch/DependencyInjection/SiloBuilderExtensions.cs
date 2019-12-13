using Microsoft.Extensions.DependencyInjection;
using Nest;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.Storage.Elasticsearch;
using System;

namespace Orleans.Hosting
{
    public static class SiloBuilderExtensions
    {
        /// <summary>
        ///  Add Elasticsearch  Storage
        /// </summary>
        /// <param name="build"><see cref="ISiloBuilder"/></param>
        /// <param name="storageName">storage Name</param>
        /// <returns></returns>
        public static ISiloBuilder AddElasticsearchStorage(this ISiloBuilder build, Action<IElasticsearchStorageBuilder> buildAction, string storageName = ElasticsearchStorage.DefaultName)
        {
            build.ConfigureServices(services =>
            {
                services.AddElasticsearchStorage(buildAction, storageName);
            });
            build.AddStartupTask<SiloBuilderStartup>();
            return build;
        }

        /// <summary>
        ///  Add Elasticsearch  Storage
        /// </summary>
        /// <param name="build"><see cref="ISiloBuilder"/></param>
        /// <param name="storageName">storage Name</param>
        /// <returns></returns>
        public static ISiloHostBuilder AddElasticsearchStorage(this ISiloHostBuilder build, Action<IElasticsearchStorageBuilder> buildAction, string storageName = ElasticsearchStorage.DefaultName)
        {
            build.ConfigureServices(services =>
            {
                services.AddElasticsearchStorage(buildAction, storageName);
            });
            build.AddStartupTask<SiloBuilderStartup>();
            return build;
        }

        
        /// <summary>
        ///  Add Elasticsearch  Storage
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="storageName">storage Name</param>
        /// <returns></returns>
        public static IServiceCollection AddElasticsearchStorage(this IServiceCollection services, Action<IElasticsearchStorageBuilder> builer, string storageName = ElasticsearchStorage.DefaultName)
        {
            services.AddTransientNamedService<IGrainStorage, GrainStorage>(storageName);
            var builder = new ElasticsearchStorageBuilder(services, storageName);
            builer.Invoke(builder);
            builder.Build();
            return services;
        }
    }
}
