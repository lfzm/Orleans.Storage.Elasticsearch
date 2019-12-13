using Orleans.Storage.Elasticsearch.Test.Mapping;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class ElasticsearchStorageInfoTest
    {
        [Fact]
        public void should_parser_success()
        {
            ElasticsearchStorageInfo storageInfo = new ElasticsearchStorageInfo(typeof(UserStorage));
            Assert.Equal("user", storageInfo.IndexName);
            Assert.Equal("doc", storageInfo.TypeName);
            Assert.Equal(typeof(UserModel).FullName, storageInfo.StateFullName);
            Assert.Equal(typeof(UserDocument), storageInfo.MapperType);
            Assert.Equal(typeof(UserStorage), storageInfo.StorageType);
        }
    }
}
