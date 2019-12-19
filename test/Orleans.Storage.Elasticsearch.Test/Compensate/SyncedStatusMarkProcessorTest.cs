using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Runtime;
using Orleans.Storage.Elasticsearch.Compensate;
using Orleans.Storage.Elasticsearch.Test.Mapping;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class SyncedStatusMarkProcessorTest
    {
        private readonly string indexName = "indexName";
        private readonly Mock<ICompensateCheckStorage<UserModel>> storage = new Mock<ICompensateCheckStorage<UserModel>>();
        private readonly SyncedStatusMarkProcessor markProcessor;
        public SyncedStatusMarkProcessorTest()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));
            services.AddTransient<ICompensateStorage<UserModel>>((sp) => storage.Object);
            services.AddOptions().Configure<ElasticsearchStorageOptions>(ElasticsearchStorage.DefaultName, opt =>
            {
                opt.MarkProcessMaxCount = 5;
                opt.MarkWaitInterval = TimeSpan.FromSeconds(1);
            });
            services.AddOptions().Configure<ElasticsearchStorageInfo>(indexName, opt =>
            {
                opt.IndexName = indexName;
                opt.ModelType = typeof(UserModel);
                opt.StorageName = ElasticsearchStorage.DefaultName;
                opt.DocumentType = typeof(UserDocument);
                opt.CompleteCheck = true;
            });
            storage.Setup(f => f.ModifySyncedStatus(null)).Returns(Task.CompletedTask);
            markProcessor = new SyncedStatusMarkProcessor(services.BuildServiceProvider(), indexName);
        }

        [Fact]
        public void should_markSynced_notice()
        {
            markProcessor.MarkSynced("1");
            Assert.Equal(1, markProcessor.WaitCount);
        }
        [Fact]
        public async Task should_processor_block_4()
        {
            Enumerable.Range(1, 14).ToList().ForEach(f =>
            {
                markProcessor.MarkSynced(f.ToString()); ;
            });
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.Equal(4, markProcessor.WaitCount);
        }
        [Fact]
        public async Task should_processor_WaitMarkComplete()
        {
            for (int i = 0; i < 101; i++)
            {
                await Task.Delay(1);
                markProcessor.MarkSynced(i.ToString());
            }
            await markProcessor.WaitMarkComplete();
            Assert.Equal(0, markProcessor.WaitCount);
        }

    }
}
