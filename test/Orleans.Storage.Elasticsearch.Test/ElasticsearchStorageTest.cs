using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nest;
using Orleans.Runtime;
using Orleans.Storage.Elasticsearch.Compensate;
using Orleans.Storage.Elasticsearch.Test.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class ElasticsearchStorageTest
    {
        private readonly ElasticsearchStorage<UserModel> storage;
        private readonly Mock<IElasticsearchClient<UserModel>> clientMock = new Mock<IElasticsearchClient<UserModel>>();
        private readonly Mock<ISyncedStatusMarkProcessor> markProcessorMock = new Mock<ISyncedStatusMarkProcessor>();

        public ElasticsearchStorageTest()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));
            services.AddSingletonNamedService<ISyncedStatusMarkProcessor>(UserDocument.IndexName, (key, sp) => markProcessorMock.Object);
            services.AddOptions().Configure<ElasticsearchStorageOptions>(ElasticsearchStorage.DefaultName, opt =>
            {
                opt.MarkProcessMaxCount = 5;
                opt.MarkWaitInterval = TimeSpan.FromSeconds(1);
            });
            services.AddOptions().Configure<ElasticsearchStorageInfo>(UserDocument.IndexName, opt =>
            {
                opt.IndexName = UserDocument.IndexName;
                opt.ModelType = typeof(UserModel);
                opt.StorageName = ElasticsearchStorage.DefaultName;
                opt.DocumentType = typeof(UserDocument);
            });
            storage = new ElasticsearchStorage<UserModel>(services.BuildServiceProvider(), UserDocument.IndexName, clientMock.Object);
        }
        [Fact]
        public async Task should_get_success()
        {
            clientMock.Setup(f => f.GetAsync("1")).Returns(Task.FromResult(new UserModel()));
            var model = await storage.GetAsync("1");
            Assert.NotNull(model);
        }

        [Fact]
        public async Task should_getlist_success()
        {
            var list = new List<UserModel>()
            {
                new UserModel(){Id=100}
            };
            clientMock.Setup(f => f.GetListAsync(It.IsAny<IEnumerable<string>>()))
                .Returns(Task.FromResult<IEnumerable<UserModel>>(list));
            var models = await storage.GetListAsync(new List<string> { "1", "2" });
            Assert.NotNull(models);
            Assert.Single(models);
            Assert.Equal(models.ToList()[0].Id, list[0].Id);
        }

        [Fact]
        public async Task should_delete_success()
        {
            clientMock.Setup(f => f.DeleteAsync("1")).Returns(Task.FromResult(true));
            markProcessorMock.Setup(f => f.MarkSynced("1"));
            var ret = await storage.DeleteAsync("1");
            Assert.True(ret);
        }

        [Fact]
        public async Task should_delete_failed()
        {
            clientMock.Setup(f => f.DeleteAsync("1")).Returns(Task.FromResult(false));
            markProcessorMock.Setup(f => f.MarkSynced("1"));
            var ret = await storage.DeleteAsync("1");
            Assert.False(ret);
        }

        [Fact]
        public async Task should_deleteMany_success()
        {
            clientMock.Setup(f => f.DeleteAsync("1")).Returns(Task.FromResult(true));
            markProcessorMock.Setup(f => f.MarkSynced("1"));
            await storage.DeleteManyAsync(new List<string>() { "1" });
        }

        [Fact]
        public async Task should_indexMany_success()
        {
            var bulkResponseMock = new Mock<IBulkResponse>();
            bulkResponseMock.Setup(f => f.IsValid).Returns(true);
            var models = new List<UserModel> { new UserModel() { Id = 100 }, new UserModel() { Id = 101 } };
            clientMock.Setup(f => f.IndexManyAsync(It.IsAny<IEnumerable<ElasticsearchDocument<UserModel>>>()))
                .Returns(Task.FromResult(bulkResponseMock.Object));
            await storage.IndexManyAsync(models);
        }

        [Fact]
        public async Task should_index_success()
        {
            var bulkResponseMock = new Mock<IBulkResponse>();
            bulkResponseMock.Setup(f => f.IsValid).Returns(true);

            clientMock.Setup(f => f.IndexManyAsync(It.IsAny<IEnumerable<ElasticsearchDocument<UserModel>>>()))
                .Returns(Task.FromResult(bulkResponseMock.Object));
            var rest = await storage.IndexAsync(new UserModel() { Id = 100 });
            Assert.True(rest);
        }

        [Fact]
        public async Task should_index_failed()
        {
            var model = new UserModel() { Id = 100 };
            var bulkResponseMock = new Mock<IBulkResponse>();
            var bulkResponseItemMock = new Mock<IBulkResponseItem>();
            bulkResponseItemMock.Setup(f => f.IsValid).Returns(false);
            bulkResponseItemMock.Setup(f => f.Id).Returns(model.Id.ToString());

            bulkResponseMock.Setup(f => f.IsValid).Returns(false);
            bulkResponseMock.Setup(f => f.Items).Returns(new List<IBulkResponseItem> { bulkResponseItemMock.Object });
            clientMock.Setup(f => f.IndexManyAsync(It.IsAny<IEnumerable<ElasticsearchDocument<UserModel>>>()))
                .Returns(Task.FromResult(bulkResponseMock.Object));
            var rest = await storage.IndexAsync(model);
            Assert.False(rest);
        }
    }
}
