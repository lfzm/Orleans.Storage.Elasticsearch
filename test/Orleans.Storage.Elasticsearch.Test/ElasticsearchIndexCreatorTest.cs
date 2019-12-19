using Elasticsearch.Net;
using Nest;
using Orleans.Storage.Elasticsearch.Test.Mapping;
using System.Collections.Generic;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class ElasticsearchIndexCreatorTest
    {
        private readonly ElasticsearchIndexCreator _indexCreator;

        public ElasticsearchIndexCreatorTest()
        {
            _indexCreator = new ElasticsearchIndexCreator(new ConnectionSettings(new InMemoryConnection()));
        }

        [Fact]
        public void should_create_index_notexist()
        {
            var infos = new List<ElasticsearchStorageInfo>
            {
                new ElasticsearchStorageInfo(UserDocument.IndexName,typeof(UserDocument),typeof(UserModel))
            };
            _indexCreator.Create(infos);
        }


        [Fact]
        public void should_create_index_exist()
        {
            this.should_create_index_notexist();

            var infos = new List<ElasticsearchStorageInfo>
            {
                new ElasticsearchStorageInfo(UserDocument.IndexName,typeof(UserDocument),typeof(UserModel))
            };
            _indexCreator.Create(infos);
        }
    }
}
