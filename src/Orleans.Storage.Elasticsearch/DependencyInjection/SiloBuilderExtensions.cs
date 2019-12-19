using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.Storage.Elasticsearch;
using Orleans.Storage.Elasticsearch.Compensate;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            build.AddStartupTask((sp, token) => Startup(sp, token, storageName));
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
            build.AddStartupTask((sp, token) => Startup(sp, token, storageName));
            return build;
        }


        private static async Task Startup(IServiceProvider serviceProvider, CancellationToken cancellationToken, string storageName)
        {
            var reminderTable = serviceProvider.GetService<IReminderTable>();
            // 需要有配置Orleans定时器
            if (reminderTable != null)
            {
                var options = serviceProvider.GetOptionsByName<ElasticsearchStorageOptions>(storageName);
                var _grainFactory = serviceProvider.GetRequiredService<IGrainFactory>();
                // 启动数据完整性检查
                foreach (var name in options.CompleteCheckIndexList)
                {
                    await _grainFactory.GetGrain<ICompensater>(name).CompletaCheckAsync();
                }
            }
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
