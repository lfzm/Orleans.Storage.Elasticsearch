using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Storage.Elasticsearch.Test.Mapping
{
    [Serializable]
    public class UserModel:IStorageModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }

        public string GetPrimaryKey()
        {
            return Id.ToString();
        }
    }
}
