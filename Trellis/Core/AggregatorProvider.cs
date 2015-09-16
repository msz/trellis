using System;
using System.Linq;

namespace Trellis.Core
{
    public class AggregatorProvider : IAggregatorProvider
    {
        ModelProvider modelProvider;
        internal protected AggregatorProvider(IDB db, ModelProvider modelProvider)
        {
            this.modelProvider = modelProvider;
        }
        public LazyAggregator Get(Type aggregatorType, Id id)
        {
            var modelTypes = LazyAggregator.usings[aggregatorType];
            var models = modelTypes.Select(x => modelProvider.Get(x, id)).ToArray();
            return LazyAggregator.New(aggregatorType, this, models);
        }

        public T Get<T>(Id id) where T : LazyAggregator
        {
            return (T)Get(typeof(T), id);
        }

        public void Save(LazyAggregator aggregator)
        {
            throw new NotImplementedException("This is temporary debug method not implemented in real database aggregator provider");
        }
    }

    public class AggregatorProvider<T> where T :LazyAggregator
    {
        AggregatorProvider provider;
        public AggregatorProvider(AggregatorProvider provider)
        {
            this.provider = provider;
        }

        public T Get(Id id)
        {
            return (T)provider.Get(typeof(T), id);
        }

        public void Save(T aggregator)
        {
            throw new NotImplementedException();
        }
    }

    public interface IAggregatorProvider
    {
        LazyAggregator Get(Type type, Id id);
        void Save(LazyAggregator aggregator);
    }

    public interface IAggregatorProvider<T> where T : LazyAggregator
    {
        T Get(Id id);
        void Save(T aggregator);
    }

    public static class AggregatorProviderExtensions
    {
        public static void Save(this IAggregatorProvider provider, params LazyAggregator[] aggregators)
        {
            foreach (var aggregator in aggregators)
            {
                provider.Save(aggregator);
            }
        }
    }
}
