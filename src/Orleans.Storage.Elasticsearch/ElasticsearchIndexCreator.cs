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
        /// 添加需要创建的索引
        /// </summary>
        /// <typeparam name="TModel">Mapper</typeparam>
        /// <param name="indexName">索引名称</param>
        public void Add<TModel>(string indexName)
            where TModel : class
        {
            this.createIndexFuncList.Add((client) => this.Create<TModel>(client,indexName));
        }

        /// <summary>
        /// 创建所有索引
        /// </summary>
        public void Create()
        {
            var client = new ElasticClient(builder.Settings);

            var results = createIndexFuncList.Select(i => i.Invoke(client)).ToList();
            Task.WaitAll(results.ToArray());
            //验证创建结果
            results.ForEach(r =>
            {
                var response = r.Result;
                if (response == null)
                    return;
                if (r.Result.IsValid)
                    return;

                if(response.TryGetServerErrorReason(out var reason))
                {
                    throw new Exception(reason);
                }
                else
                {
                    throw new Exception("Create Elasticsearch index failed");
                }
            });

        }

        private async Task<ICreateIndexResponse> Create<TModel>(IElasticClient client, string indexName)
             where TModel : class
        {
            var response =await client.GetIndexAsync(indexName);
            if (!response.IsValid)
            {
                return await client.CreateIndexAsync(indexName, c => c.Mappings(ms => ms.Map<TModel>(m => m.AutoMap())));
            }
            else
                return null;
        }
    }
}
