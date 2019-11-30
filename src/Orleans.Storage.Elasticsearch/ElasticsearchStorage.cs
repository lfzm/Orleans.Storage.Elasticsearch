using Microsoft.Extensions.DependencyInjection;
using Nest;
using Orleans.Storage.Elasticsearch.Compensate;
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

        public async Task<bool> ClearAsync(string id)
        {
            var response = await this.DeleteAsync(id);
            if (response.IsValid)
                return true;
            else
            {
                // response filed handle
                this.ServiceProvider.GetRequiredService<ElasticsearchResponseFailedHandle>().Handle(response);
                // Data compensation
                if (this.EnsureReminderServiceRegistered())
                {
                    await this.ServiceProvider.GetRequiredService<IGrainFactory>()
                        .GetGrain<ICompensateGrain>(typeof(TEntity).FullName)
                        .WriteAsync(id, CompensateType.Clear);
                }
                return false;
            }
        }
        public async Task<object> ReadAsync(string id)
        {
            return await this.QueryAsync(id);
        }
        public async Task<bool> WriteAsync(string id, object obj)
        {
            if (obj is TEntity entity)
            {
                var response = await this.IndexAsync(entity);
                if (!response.IsValid)
                {
                    // response filed handle
                    this.ServiceProvider.GetRequiredService<ElasticsearchResponseFailedHandle>().Handle(response);
                    // Data compensation
                    if (this.EnsureReminderServiceRegistered())
                    {
                        await this.ServiceProvider.GetRequiredService<IGrainFactory>()
                            .GetGrain<ICompensateGrain>(typeof(TEntity).FullName)
                            .WriteAsync(id, CompensateType.Write);
                    }
                    return false;
                }
                else
                    return true;
            }
            else
                throw new Exception($"WriteAsync：entity is not the same type as {typeof(TEntity).Name}");
        }

        private bool EnsureReminderServiceRegistered()
        {
            var reminderTable = this.ServiceProvider.GetService<IReminderTable>();
            if (reminderTable == null)
                return false;
            else
                return true;
        }


        public abstract Task<IIndexResponse> IndexAsync(TEntity entity);
        public abstract Task<TEntity> QueryAsync(string id);
        public abstract Task<IDeleteResponse> DeleteAsync(string id);
        public abstract Task<bool> RefreshAsync(string id);
    }

    public class ElasticsearchStorage
    {
        /// <summary>
        /// 默认 Elasticsearch 存储
        /// </summary>
        public const string DefaultName = "ElasticsearchStorage";
    }
}
