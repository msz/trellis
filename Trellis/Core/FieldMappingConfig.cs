using System;
using System.Linq;
using System.Linq.Expressions;
using Trellis.Utils;

namespace Trellis.Core
{
    public class FieldMappingConfig<TAgg, TField> where TAgg : LazyAggregator
    {
        internal MappingConfig<TAgg> mappingConfig;
        internal string fieldName;
        internal FieldMappingConfig(MappingConfig<TAgg> parent, string fieldName)
        {
            this.mappingConfig = parent;
            this.fieldName = fieldName;
        }

        public FieldMappingConfig<TAgg, TField> From(Func<AggregatorContext, object> valueFunc)
        {
            mappingConfig.MapToField(fieldName, valueFunc);
            return this;
        }

        public MappingConfig<TAgg> Using<TModel>(params Expression<Func<TModel, object>>[] fieldExps) where TModel : LazyModel
        {
            var type = typeof(TModel);
            var names = fieldExps.Select(x => type.GetPropertyInfo(x).Name).ToArray();
            mappingConfig.FieldUsing(fieldName, type, names);
            return mappingConfig;
        }

        public ToModelFieldMappingConfig<TAgg, TField, TModel, TModelField> To<TModel,TModelField>(
            Expression<Func<TModel, TModelField>> fieldSelector)
            where TModel : LazyModel
        {
            var modelType = typeof(TModel);
            var targetFieldName = modelType.GetPropertyInfo(fieldSelector).Name;
            return new ToModelFieldMappingConfig<TAgg, TField, TModel, TModelField>(this, targetFieldName);
        }

        public MappingConfig<TAgg> OneToOne<TModel>(Expression<Func<TModel, object>> fieldSelector) where TModel : LazyModel
        {
            var modelType = typeof(TModel);
            var targetFieldName = modelType.GetPropertyInfo(fieldSelector).Name;
            return mappingConfig.FieldOneToOne(fieldName, modelType, targetFieldName);
        }
    }
}
