using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Storage.Elasticsearch.Compensate;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch
{
    public class SiloBuilderStartup : IStartupTask
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ElasticsearchStorageOptions _options;

        public SiloBuilderStartup(IGrainFactory grainFactory, IOptions<ElasticsearchStorageOptions> options)
        {
            this._grainFactory = grainFactory;
            this._options = options?.Value;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            // 启动数据完整性检查
            foreach (var name in _options?.CompleteCheckIndexList)
            {
                await this._grainFactory.GetGrain<ICompensater>(name).CompletaCheckAsync();
            }
        }
    }
}
