using Nest;
using Orleans.Storage.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grain
{
    [ElasticsearchType(Name = "user")]
    public class UserModel : IElasticsearchModel
    {
        public const string IndexName = "orleans-user";

        public int Id { get; set; }

        public string Name { get; set; }

        public string Sex { get; set; }

        public string GetPrimaryKey()
        {
            return this.Id.ToString();
        }
    }
}
