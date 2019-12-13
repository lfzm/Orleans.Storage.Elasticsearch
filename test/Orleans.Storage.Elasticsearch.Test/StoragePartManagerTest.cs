using Orleans.Storage.Elasticsearch.Test.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class StoragePartManagerTest
    {
        [Fact]
        public void GetStorageInfoList()
        {
            var manager = new StoragePartManager(new List<Assembly> { typeof(UserDocument).Assembly }.ToArray());
            var list = manager.GetStorageInfoList();
            Assert.NotNull(list);
            Assert.Equal(2,list.Count);
        }
    }
}
