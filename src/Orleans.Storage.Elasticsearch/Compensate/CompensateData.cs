using System;

namespace Orleans.Storage.Elasticsearch.Compensate
{
    /// <summary>
    /// 需要补偿数据
    /// </summary>
    public class CompensateData
    {
        public CompensateData()
        {

        }
        public CompensateData(string id, CompensateType type,string indexName)
        {
            Id = id;
            Type = type;
            IndexName = indexName;
        }
        public CompensateData(string id, CompensateType type)
        {
            Id = id;
            Type = type;
        }
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 数据版本号
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 补偿类型
        /// </summary>
        public CompensateType Type { get; set; }
        /// <summary>
        /// index name
        /// </summary>
        public string IndexName { get; set; }
        public override string ToString()
        {
            return $"{Id}|{(int)Type}";
        }
        /// <summary>
        /// ToString 的字符串转换为对象
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static CompensateData From(string text)
        {
            var data = text.Split('|');
            var id = data[0];
            var type = int.Parse(data[1]);
            return new CompensateData(id, (CompensateType)type);
        }
    }
}
