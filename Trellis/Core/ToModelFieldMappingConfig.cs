using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trellis.Core
{
    public class ToModelFieldMappingConfig<TAgg, TField, TModel, TModelField>
        where TAgg :LazyAggregator
        where TModel :LazyModel
    {
        internal FieldMappingConfig<TAgg, TField> fieldMappingConfig;
        internal string modelFieldName;
        internal ToModelFieldMappingConfig(FieldMappingConfig<TAgg, TField> parent, string modelFieldName)
        {
            fieldMappingConfig = parent;
            this.modelFieldName = modelFieldName;
        }

        public FieldMappingConfig<TAgg, TField> With(Func<TField, TModelField> valueTransform)
        {
            fieldMappingConfig.mappingConfig.MapFromField(fieldMappingConfig.fieldName, typeof(TModel), modelFieldName, x => valueTransform((TField)x));
            return fieldMappingConfig;
        }
    }
}
