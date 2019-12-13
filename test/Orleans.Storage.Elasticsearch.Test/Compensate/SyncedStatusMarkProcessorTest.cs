using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Runtime;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class SyncedStatusMarkProcessorTest
    {
        private readonly string indexName = "indexName";
        private readonly Mock<IElasticsearchCompleteCheckStorage> storage = new Mock<IElasticsearchCompleteCheckStorage>();
        private readonly StorageSyncedMark mpsc;
        public SyncedStatusMarkProcessorTest()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));
            services.AddTransientNamedService<IElasticsearchCompleteCheckStorage>(indexName, (key, sp) => storage.Object);
            services.Configure<ElasticsearchStorageOptions>(ElasticsearchStorage.DefaultName, opt =>
            {
                opt.MarkProcessMaxCount = 5;
                opt.MarkWaitInterval = TimeSpan.FromSeconds(10);
            });
            mpsc = new StorageSyncedMark(services.BuildServiceProvider(), indexName, ElasticsearchStorage.DefaultName);
            storage.Setup(f => f.MarkSyncedAsync(null)).Returns(Task.CompletedTask);
        }

        [Fact]
        public void should_markSyncedAsync_notice()
        {
            mpsc.MarkSyncedAsync("1");
            Assert.Equal(1, mpsc.WaitCount);
        }
        [Fact]
        public async Task should_processor_block_4()
        {
            Enumerable.Range(1, 14).ToList().ForEach(f =>
            {
                mpsc.MarkSyncedAsync(f.ToString()); ;
            });
            await Task.Delay(6);
            Assert.Equal(4, mpsc.WaitCount);
        }
        [Fact]
        public async Task should_should_processor_clearQueue()
        {
            for (int i = 1; i < 101; i++)
            {
                await Task.Delay(1);
                mpsc.MarkSyncedAsync(i.ToString());
            }
            await mpsc.MarkSynced();
            Assert.Equal(0, mpsc.WaitCount);
        }

    }
}
