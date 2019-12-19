using Microsoft.Extensions.DependencyInjection;
using Nest;
using Orleans.Storage.Elasticsearch.Test.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Storage.Elasticsearch.Test
{
    public class ElasticsearchClientTest
    {
        public readonly ElasticsearchClient<UserDocument> elasticsearchClient;
        public ElasticsearchClientTest()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            var client = new ElasticClient(new ConnectionSettings(new Uri("http://")));
            this.elasticsearchClient = new ElasticsearchClient<UserDocument>(services.BuildServiceProvider(), client, UserDocument.IndexName, "doc");
        }
        [Fact]
        public async Task should_index_get_delete_sccuess()
        {
            // index
            var doc = new ElasticsearchDocument<UserDocument>(new UserDocument()
            {
                Id = 1,
                Name = "ces",
                Sex = "男"
            }, "1");
            var ret = await elasticsearchClient.IndexAsync(doc);
            Assert.True(ret.IsValid);

            // get
            var doc1 = await elasticsearchClient.GetAsync("1");
            this.Comparison(doc, doc1);

            // delete
            var ret1 = await elasticsearchClient.DeleteAsync("1");
            Assert.True(ret1);
        }

        [Fact]
        public async Task should_indexMany_getList_deleteMany_success()
        {
            // index
            var ids = new List<string>() { "3", "4" };
            var doc1 = new ElasticsearchDocument<UserDocument>(new UserDocument() { Id = 3, Name = "ces", Sex = "男" }, "3",10);
            var doc2 = new ElasticsearchDocument<UserDocument>(new UserDocument() { Id = 4, Name = "ces", Sex = "女" }, "4");
            var docs = new List<ElasticsearchDocument<UserDocument>>() { doc1, doc2 };
            var resp = await elasticsearchClient.IndexManyAsync(docs);
            Assert.True(resp.IsValid);

            // getList
            var _docs = await elasticsearchClient.GetListAsync(ids);
            foreach (var doc in docs)
            {
                var d = _docs.FirstOrDefault(f => f.Id == doc.Document.Id);
                Assert.NotNull(d);
                this.Comparison(doc, d);
            }

            // delte Many
            resp = await elasticsearchClient.DeleteManyAsync(ids);
            Assert.True(resp.IsValid);
        }


        [Fact]
        public async Task should_delete_failed()
        {
            var ret = await elasticsearchClient.DeleteAsync("1000");
            Assert.False(ret);
        }

        [Fact]
        public async Task should_index_version_sccuess()
        {
            // index
            var doc = new ElasticsearchDocument<UserDocument>(new UserDocument()
            {
                Id = 5,
                Name = "ces",
                Sex = "男"
            }, "5", 101);
            var ret = await elasticsearchClient.IndexAsync(doc);
            Assert.True(ret.IsValid);

            // get version
            var doc1 = await elasticsearchClient.GetVersionListAsync(new List<string> { "5" });
            Assert.True(doc1.ContainsKey(doc.PrimaryKey));
            var v = doc1[doc.PrimaryKey];
            Assert.Equal(doc.VersionNo, v);

            await elasticsearchClient.DeleteAsync(doc.PrimaryKey);
        }


        private void Comparison(ElasticsearchDocument<UserDocument> doc, UserDocument doc1)
        {
            Assert.NotNull(doc1);
            Assert.Equal(doc.Document.Id, doc1.Id);
            Assert.Equal(doc.Document.Name, doc1.Name);
            Assert.Equal(doc.Document.Sex, doc1.Sex);

        }
    }
}
