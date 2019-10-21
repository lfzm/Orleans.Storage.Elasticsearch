using Nest;
using System;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public abstract class ElasticsearchStorage<TEntity> : IElasticsearchStorage
        where TEntity : class
    {
        private readonly IServiceProvider ServiceProvider;
        public IElasticClient Client { get; set; }

        protected ElasticsearchStorage(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public async Task ClearAsync(string id)
        {
            var response = await this.DeleteAsync(id);
            if (response.IsValid)
                return;
            else
            {
                if (response.TryGetServerErrorReason(out var reason))
                    throw new Exception(reason);
                else
                {
                    throw new Exception("Requesting ES failed");
                }
            }
        }
        public async Task<object> ReadAsync(string id)
        {
            return await this.QueryAsync(id);
        }
        public async Task<object> WriteAsync(object obj)
        {
            if (obj is TEntity entity)
            {
                var response = await this.IndexAsync(entity);
                if (response.IsValid)
                    return obj;
                else
                {
                    if (response.TryGetServerErrorReason(out var reason))
                        throw new Exception(reason);
                    else
                    {
                        throw new Exception("Requesting ES failed");
                    }
                }
            }
            else
                throw new Exception($"WriteAsync：entity is not the same type as {typeof(TEntity).Name}");
        }

        public abstract Task<IIndexResponse> IndexAsync(TEntity entity);

        public abstract Task<TEntity> QueryAsync(string id);

        public abstract Task<IDeleteResponse> DeleteAsync(string id);

        public abstract Task<bool> RefreshAsync(string id);

    }

    public class ElasticsearchStorage
    {
        public const string DefaultName = "ElasticsearchStorage";
    }
}
