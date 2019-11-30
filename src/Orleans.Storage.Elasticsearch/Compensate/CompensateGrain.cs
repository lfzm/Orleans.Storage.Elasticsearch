using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public class CompensateGrain : Grain, ICompensateGrain, IRemindable
    {
        private readonly ILogger _logger;
        private IGrainReminder reminder;

        public CompensateGrain(ILogger<CompensateGrain> logger)
        {
            _logger = logger;
        }

        public async Task<int> CompensateAsync(List<string> ids, CompensateType type)
        {
            if (ids == null || ids.Count == 0)
                return 0;

            int count = 0;
            var fullName = this.GetPrimaryKeyString();
            foreach (var id in ids)
            {
                if (type.Equals(CompensateType.Clear.ToString()))
                {
                    if (await this.ServiceProvider.GetRequiredServiceByName<IElasticsearchStorage>(fullName).ClearAsync(id))
                        count++;
                }
                else
                {
                    if (await this.ServiceProvider.GetRequiredServiceByName<IElasticsearchStorage>(fullName).RefreshAsync(id))
                        count++;
                }
            }
            return count;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            var data = reminderName.Split('|');
            var id = data[0];
            var type = data[1];
            var fullName = this.GetPrimaryKeyString();

            if (type.Equals(CompensateType.Clear.ToString()))
            {
                if (!await this.ServiceProvider.GetRequiredServiceByName<IElasticsearchStorage>(fullName).ClearAsync(id))
                {
                    this._logger.LogInformation($"{fullName} {reminderName} Compensate clear filed");
                    return;
                }
            }
            else
            {
                if (!await this.ServiceProvider.GetRequiredServiceByName<IElasticsearchStorage>(fullName).RefreshAsync(id))
                {
                    this._logger.LogInformation($"{fullName} {reminderName} Compensate refresh filed");
                    return;
                }
            }
            if (reminder == null)
                reminder = await this.GetReminder(reminderName);
            if (reminder != null)
                await this.UnregisterReminder(reminder);
            return;
        }

        public async Task WriteAsync(string id, CompensateType type)
        {
            reminder = await this.RegisterOrUpdateReminder($"{id}|{type.ToString()}", TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
        }
    }
}
