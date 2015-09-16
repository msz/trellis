
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Trellis.Core;
using Trellis.Tests.Mocks;

namespace Trellis.Tests.Core
{
    [TestFixture]
    class LazyModelTests
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

        DbCollectionMockStorage storage;
        Mock<IDBCollection> dBCollectionMock;
        
        [SetUp]
        public void SetUp()
        {
            storage = new DbCollectionMockStorage();
            dBCollectionMock = MockProvider.GetDBCollectionMock(storage);
        }

        [Test]
        public void Field_loading_test()
        {
            // arrange
            var field1 = "test";
            var field2 = DateTime.UtcNow;
            storage[0] = new Dictionary<string, object>{
                {"Field1", field1},
                {"Field2", field2}
            };
            var foo = new Foo(0, dBCollectionMock.Object);

            // act
            var val1 = foo.Field1;
            var val2 = foo.Field2;

            // assert
            val1.Should().Be(field1);
            val2.Should().Be(field2);
            dBCollectionMock.Verify(x => 
                x.GetModelField(0, It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Setting_field_does_not_interact_with_DB()
        {
            // arrange
            storage[0] = new Dictionary<string, object>{
                {"Field1", default(string)},
                {"Field2", default(DateTime)}
            };
            var foo = new Foo(0, dBCollectionMock.Object);

            // act
            foo.Field1 = "test";
            foo.Field2 = DateTime.UtcNow;

            // assert
            storage[0]["Field1"].Should().Be(default(string));
            storage[0]["Field2"].Should().Be(default(DateTime));
        }

        [Test]
        public void Commit_applies_changes_to_model()
        {
            // arrange
            storage[0] = new Dictionary<string, object>{
                {"Field1", default(string)},
                {"Field2", default(DateTime)}
            };
            var foo = new Foo(0, dBCollectionMock.Object);

            // act
            var field1 = "test";
            var field2 = DateTime.UtcNow;
            foo.Field1 = field1;
            foo.Field2 = field2;
            foo.Commit();

            // assert
            storage[0]["Field1"].Should().Be(field1);
            storage[0]["Field2"].Should().Be(field2);
            dBCollectionMock.Verify(x => 
                x.UpdateFields(0, It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [Test]
        public void Preload_test()
        {
            // arrange
            var field1 = "test";
            var field2 = DateTime.UtcNow;
            storage[0] = new Dictionary<string, object>{
                {"Field1", field1},
                {"Field2", field2}
            };
            var foo = new Foo(0, dBCollectionMock.Object);

            // act
            foo.Preload(x => x.Field1);
            var val1 = foo.Field1;
            var val2 = foo.Field2;

            // assert
            val1.Should().Be(field1);
            val2.Should().Be(field2);
            dBCollectionMock.Verify(x =>
                x.GetModelField(0, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void AreSetEventEnabled_disables_events()
        {
            // arrange
            var prevField1 = default(string);
            var prevField2 = default(DateTime);
            var field1 = "test";
            var field2 = DateTime.UtcNow;
            storage[0] = new Dictionary<string, object>{
                {"Field1", prevField1},
                {"Field2", prevField2}
            };
            var foo = new Foo(0, dBCollectionMock.Object);

            // act
            foo.AreSetEventsEnabled = false;
            foo.Field1 = field1;
            foo.Field2 = field2;
            foo.Commit();

            // assert
            storage[0]["Field1"].Should().Be(prevField1);
            storage[0]["Field2"].Should().Be(prevField2);
            foo.Field1.Should().Be(field1);
            foo.Field2.Should().Be(field2);
        }
    }
}
