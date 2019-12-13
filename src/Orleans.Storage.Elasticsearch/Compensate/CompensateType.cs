using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    public enum CompensateType
    {
        Write=1,
        Clear=0
    }
}
