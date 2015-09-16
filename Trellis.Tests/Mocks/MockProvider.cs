using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Trellis.Core;
using Trellis.Utils;

namespace Trellis.Tests.Mocks
{
    public static class MockProvider
    {
        public static Mock<IDBCollection> GetDBCollectionMock(DbCollectionMockStorage storage)
        {
            var dBCollectionMock = new Mock<IDBCollection>(MockBehavior.Strict);

            dBCollectionMock.Setup(x =>
                x.GetModelField(It.IsAny<Id>(), It.IsAny<string>()))
                .Returns((Id id, string fieldName) =>
                    storage[id][fieldName]);

            dBCollectionMock.Setup(x =>
                x.UpdateFields(It.IsAny<Id>(), It.IsNotNull<IDictionary<string, object>>()))
                .Callback((Id id, IDictionary<string, object> fieldVals) => 
                    storage[id].Update(fieldVals));

            dBCollectionMock.Setup(x =>
                x.GetFields(It.IsAny<Id>(), It.IsNotNull<string[]>()))
                .Returns((Id id, string[] fieldNames) =>
                    fieldNames.ToDictionary(x => x, x => storage[id][x]));

            dBCollectionMock.Setup(x =>
                x.ArrayElem(It.IsNotNull<Id>(), It.IsNotNull<string>(), It.IsAny<int>()))
                .Returns((Id id, string name, int i) =>
                    ((IEnumerable)(storage[id][name])).Cast<object>().ElementAt(i));

            return dBCollectionMock;
        }

        public static Mock<IAggregatorProvider> GetAggregatorProviderMock(params LazyAggregator[] aggregators)
        {
            var storage = new DefaultDictionary<Type, DefaultDictionary<Id, LazyAggregator>>(()=>new DefaultDictionary<Id, LazyAggregator>());
            foreach(var aggregator in aggregators)
            {
                storage[aggregator.GetType()][aggregator.Id] = aggregator;
            }
            var mock = new Mock<IAggregatorProvider>();

            mock.Setup(x => x.Get(It.IsAny<Type>(), It.IsAny<Id>()))
                .Returns((Type aggType, Id id) =>
                    storage[aggType][id]);

            mock.Setup(x => x.Save(It.IsAny<LazyAggregator>()))
                .Callback((LazyAggregator agg) =>
                    storage[agg.GetType()][agg.Id] = agg);

            return mock;
        }
    }
}
