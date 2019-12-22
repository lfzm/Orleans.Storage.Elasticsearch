using System;
using System.Collections.Generic;
using System.Text;
using Orleans.Storage.Elasticsearch;

namespace Grain
{
    public class AccountModel : IElasticsearchModel
    {
        public int Id { get; set; }
        public string Username { get; set; }

        public string Password { get; set; }

        public string GetPrimaryKey()
        {
            return this.Id.ToString();
        }
    }
}
