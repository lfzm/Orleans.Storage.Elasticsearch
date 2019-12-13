using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Runtime;
using Orleans.Storage.Elasticsearch.Test.Mapping;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class GrainStorageTest
    {
        private readonly Mock<IElasticsearchStorage> storageMock = new Mock<IElasticsearchStorage>();
        private readonly Mock<IGrainState> stateMock = new Mock<IGrainState>();
        private readonly GrainStorage _storage;

        public GrainStorageTest()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));
            services.AddTransientNamedService<IElasticsearchStorage>(typeof(UserModel).FullName, (sp, key) =>
            {
                return storageMock.Object;
            });
            _storage = new GrainStorage(services.BuildServiceProvider());
        }

        [Fact]
        public void should_getRepository_success()
        {
            stateMock.Setup(s => s.State).Returns(new UserModel());
            var storage =  _storage.GetRepository(stateMock.Object);
            Assert.NotNull(storage);
        }


    }
}
