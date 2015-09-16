using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trellis.Core
{
    public class AggregatorContext
    {
        private LazyAggregator agg;
        internal AggregatorContext(LazyAggregator agg)
        {
            this.agg = agg;
        }

        public T M<T>() where T :LazyModel
        {
            return agg.Model<T>();
        }

        public LazyModel M(Type modelType)
        {
            return agg.Model(modelType);
        }
    }
}
