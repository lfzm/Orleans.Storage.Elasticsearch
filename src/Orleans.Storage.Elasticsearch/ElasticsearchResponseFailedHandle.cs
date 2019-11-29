using Microsoft.Extensions.Logging;
using Nest;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// 请求 Elasticsearch 响应失败处理
    /// </summary>
    public class ElasticsearchResponseFailedHandle
    {
        private readonly ILogger logger;

        public ElasticsearchResponseFailedHandle(ILogger<ElasticsearchResponseFailedHandle> logger)
        {
            this.logger = logger;
        }

        public void Handle(IResponse response)
        {
            if (response.TryGetServerErrorReason(out var reason))
            {
                this.logger.LogError($"request elasticsearch filed ; reason : {reason}");
            }
            else if (response.OriginalException != null)
            {
                this.logger.LogError(response.OriginalException, "request elasticsearch filed");
            }
            else
            {
                this.logger.LogError($"request elasticsearch filed ");
            }
        }
    }
}
