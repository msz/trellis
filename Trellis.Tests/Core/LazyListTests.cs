using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Trellis.Core;
using Trellis.Tests.Mocks;

namespace Trellis.Tests.Core
{
    [TestFixture]
    public class LazyListTests
    {
        DbCollectionMockStorage storage;
        Mock<IDBCollection> dbCollectionMock;

        [SetUp]
        public void SetUp()
        {
            storage = new DbCollectionMockStorage();
            dbCollectionMock = MockProvider.GetDBCollectionMock(storage);
        }

        [Test]
        public void Index_get_test()
        {
            // arrange
            storage[0] = new Dictionary<string, object>
            {
                {"List", new List<int> {1,2,3 } }
            };
            var list = new LazyList<int>(dbCollectionMock.Object, 0, "List");

            // act and assert
            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
            dbCollectionMock.Verify(x => x.ArrayElem(0, "List", It.IsAny<int>())
                , Times.Exactly(3));
        }

        [Test]
        public void Index_set_does_not_change_storage()
        {
            // arrange
            storage[0] = new Dictionary<string, object>
            {
                {"List", new List<int> {1,2,3 } }
            };
            var list = new LazyList<int>(dbCollectionMock.Object, 0, "List");

            // act
            list[1] = 69;

            // assert
            list[1].Should().Be(69);
            ((storage[0]["List"]) as IEnumerable<int>)
                .ElementAt(1)
                .Should().Be(2);
        }

        [Test]
        public void Commit_test()
        {
            // arrange
            storage[0] = new Dictionary<string, object>
            {
                {"List", new List<int> {1,2,3 } }
            };
            var list = new LazyList<int>(dbCollectionMock.Object, 0, "List");

            // act
            list[0] = 69;
            list[1] = 69;
            list[2] = 69;
            list.Commit();

            // assert
            var storageList = ((storage[0]["List"]) as IEnumerable<int>);
            storageList.ElementAt(0).Should().Be(69);
            storageList.ElementAt(1).Should().Be(69);
            storageList.ElementAt(2).Should().Be(69);

            dbCollectionMock.Verify(x => x.ArrayElem(0, "List", It.IsAny<int>()), Times.Never);
        }
    }
}
