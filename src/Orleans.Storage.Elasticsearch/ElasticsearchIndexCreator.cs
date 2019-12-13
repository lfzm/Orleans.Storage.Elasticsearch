using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public class ElasticsearchIndexCreator
    {
        private readonly ElasticsearchStorageBuilder builder;
        private List<Func<IElasticClient, Task<ICreateIndexResponse>>> createIndexFuncList;

        public ElasticsearchIndexCreator(ElasticsearchStorageBuilder builder)
        {
            this.builder = builder;
            this.createIndexFuncList = new List<Func<IElasticClient, Task<ICreateIndexResponse>>>();
        }

        /// <summary>
        /// 创建所有索引
        /// </summary>
        public void Create(List<ElasticsearchStorageInfo> storageInfos)
        {
            var client = new ElasticClient(builder.Settings);
            var results = storageInfos.Select(f => this.Create(client, f.IndexName, f.DocumentType)).ToArray();
            Task.WaitAll(results);
            //验证创建结果
            results.ToList().ForEach(r =>
            {
                var response = r.Result;
                if (response == null)
                    return;
                if (r.Result.IsValid)
                    return;

                if (response.TryGetServerErrorReason(out var reason))
                {
                    throw new Exception(response.ServerError.Status + reason);
                }
                else if (response.OriginalException != null)
                {
                    throw response.OriginalException;
                }
                else
                {
                    throw new Exception($"Create Elasticsearch index failed");
                }
            });

        }

        private async Task<ICreateIndexResponse> Create(IElasticClient client, string indexName, Type mapper)
        {
            var response = await client.GetIndexAsync(indexName);
            if (!response.IsValid)
            {
                return await client.CreateIndexAsync(indexName, c =>
                {
                    return c.Mappings(ms => ms.Map(TypeName.Create(mapper), m => m.AutoMap(mapper)));
                });
            }
            else
                return null;
        }
    }
}
