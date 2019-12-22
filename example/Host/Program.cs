using Microsoft.Extensions.Logging;
using Grain;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Threading.Tasks;
using Nest;
using Orleans.Storage.Elasticsearch;
using AutoMapper;

namespace Host
{
    class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .AddElasticsearchStorage(opt =>
                {
                    opt.ConfigureConnection(new ConnectionSettings(new Uri("http://localhost:9200")));
                    opt.AddStorage<UserModel>(UserModel.IndexName);
                    opt.AddDocumentConverter<ElasticsearchDocumentConverter>();
                    opt.AddMapperStorage<AccountModel, AccountDocument>(AccountDocument.IndexName);

                }, ElasticsearchStorage.DefaultName)
                .UseLocalhostClustering()
                .ConfigureServices(services =>
                {
                    services.AddAutoMapper(typeof(ElasticsearchDocumentConverter).Assembly);
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "A";
                    options.ServiceId = "AApp";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IUserGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole());

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
