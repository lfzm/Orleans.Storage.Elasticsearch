using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Orleans.Storage.Elasticsearch.Test.Mapping
{
    [Serializable]
    [Nest.ElasticsearchType(Name = "doc")]
    public class UserDocument
    {
        public const string IndexName = "user";

        [Nest.Keyword(Index = true)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
    }
}
