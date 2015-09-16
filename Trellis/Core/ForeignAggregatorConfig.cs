using System;
using System.Linq.Expressions;
using Trellis.Utils;

namespace Trellis.Core
{
    public class ForeignAggregatorConfig<TAgg> where TAgg : LazyAggregator
    {
        private MappingConfig<TAgg> parent;
        private string fieldName;

        public ForeignAggregatorConfig(MappingConfig<TAgg> parent, string fieldName)
        {
            this.parent = parent;
            this.fieldName = fieldName;
        }

        public MappingConfig<TAgg> IdFrom<TModel>(Expression<Func<TModel, Id>> fieldExp) where TModel :LazyModel
        {
            var modelFieldName = typeof(TModel).GetPropertyInfo(fieldExp).Name;
            parent.ForeignAggregator(fieldName, typeof(TModel), modelFieldName);
            return parent;
        }
    }
}