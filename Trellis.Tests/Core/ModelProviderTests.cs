using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Trellis.Core;
using Trellis.Tests.Mocks;

namespace Trellis.Tests.Core
{
    [TestFixture]
    public class ModelProviderTests
    {
        class Foo : LazyModel
        {
            public string Field1
            {
                get { return PropertyGetter("Field1"); }
                set { PropertySetter("Field1", value); }
            }
            public DateTime Field2
            {
                get { return PropertyGetter("Field2"); }
                set { PropertySetter("Field2", value); }
            }

            public Foo(Id id, IDBCollection _dbCollection) : base(id, _dbCollection) { }
        }

        DBCollectionMockStorage storage;
        Mock<IDBCollection> dbCollectionMock;
        Mock<IDB> dbMock;

        [SetUp]
        public void SetUp()
        {
            storage = new DBCollectionMockStorage();
            dbCollectionMock = MockProvider.GetDBCollectionMock(storage);
            dbMock = new Mock<IDB>();
            dbMock.Setup(x => x.GetCollection(It.Is<string>(y => y.Equals("Foos"))))
                .Returns(dbCollectionMock.Object);
        }

        private ModelProvider<Foo> GetProvider()
        {
            return new ModelProvider<Foo>(new ModelProvider(dbMock.Object, null));
        }

        [Test]
        public void Get_test()
        {
            // arrange
            var field1 = "test";
            var field2 = DateTime.UtcNow;
            storage[0] = new Dictionary<string, object>
            {
                { "Field1", field1 },
                { "Field2", field2 }
            };
            var provider = GetProvider();

            // act
            var foo = provider.Get(0);

            // assert
            foo.Field1.Should().Be(field1);
        }
    }
}
