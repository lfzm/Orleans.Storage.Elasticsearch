using AutoMapper;
using Orleans.Storage.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grain
{

    public class ModelMapper :Profile
    {
        public ModelMapper()
        {
            this.CreateMap<AccountDocument, AccountModel>().ReverseMap();
        }

    }
    public class ElasticsearchDocumentConverter : IStorageDocumentConverter
    {
        private readonly IMapper _mapper;

        public ElasticsearchDocumentConverter(IMapper mapper)
        {
            _mapper = mapper;
        }

        public TDocument ToDocument<TDocument, TModel>(TModel model)
        {
            return _mapper.Map<TDocument>(model);
        }

        public IEnumerable<TDocument> ToDocumentList<TDocument, TModel>(IEnumerable<TModel> models)
        {
            return _mapper.Map<List<TDocument>>(models);
        }

        public TModel ToModel<TModel, TDocument>(TDocument document)
        {
            return _mapper.Map<TModel>(document);
        }

        public IEnumerable<TModel> ToModelList<TModel, TDocument>(IEnumerable<TDocument> documents)
        {
            return _mapper.Map<List<TModel>>(documents);
        }
    }
}
