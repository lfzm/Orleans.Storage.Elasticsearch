using System.Collections.Generic;

namespace Orleans.Storage.Elasticsearch
{
    /// <summary>
    /// Elasticsearch 文档对象与模型转换器
    /// </summary>
    public interface IStorageDocumentConverter
    {
        TDocument ToDocument<TDocument, TModel>(TModel model);
        TModel ToModel<TModel, TDocument>(TDocument document);
        IEnumerable<TDocument> ToDocumentList<TDocument, TModel>(IEnumerable<TModel> models);
        IEnumerable<TModel> ToModelList<TModel, TDocument>(IEnumerable<TDocument> documents);
    }
}
