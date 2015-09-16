using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Trellis.Core;
using Trellis.Tests.Mocks;

namespace Trellis.Tests.Core
{
    [TestFixture]
    public class LazyAggregatorTests
    {
        class Processor : LazyModel
        {
            public Processor(Id id, IDBCollection coll) 
                :base(id, coll) { }
            public int Speed
            {
                get { return PropertyGetter("Speed"); }
                set { PropertySetter("Speed", value); }
            }
            public string Maker
            {
                get { return PropertyGetter("Maker"); }
                set { PropertySetter("Maker", value); }
            }
        }
        class RAM : LazyModel
        {
            public RAM(Id id, IDBCollection coll) 
                :base(id, coll) { }
            public int Size
            {
                get { return PropertyGetter("Size"); }
                set { PropertySetter("Size", value); }
            }
        }
        class Computer : LazyAggregator
        {
            static Computer()
            {
                UsingModel<Computer, Processor>();
                UsingModel<Computer, RAM>();

                Setup<Computer>()
                    .Field(x => x.ProcessorSpeed)
                        .From(x => x.M<Processor>().Speed)
                        .To<Processor, int>(x => x.Speed).With(x => x)
                        .Using<Processor>(x => x.Speed)
                    .Field(x => x.RAMSize)
                        .From(x => x.M<RAM>().Size)
                        .To<RAM, int>(x => x.Size).With(x => x)
                        .Using<RAM>(x => x.Size);
            }
            public Computer(IAggregatorProvider aggProvider, Processor processor, RAM ram) 
                : base(aggProvider, processor, ram)
            { }

            public string Maker
            {
                get { return PropertyGetter<string>("Maker"); }
                set { PropertySetter("Maker", value); }
            }
            public int ProcessorSpeed
            {
                get { return PropertyGetter<int>("ProcessorSpeed"); }
                set { PropertySetter("ProcessorSpeed", value); }
            }
            public int RAMSize
            {
                get { return PropertyGetter<int>("RAMSize"); }
                set { PropertySetter("RAMSize", value); }
            }
        }

        DbCollectionMockStorage processors;
        Mock<IDBCollection> processorCollectionMock;
        DbCollectionMockStorage rams;
        Mock<IDBCollection> ramCollectionMock;
        Mock<IAggregatorProvider> aggregatorProviderMock;

        [SetUp]
        public void SetUp()
        {
            processors = new DbCollectionMockStorage();
            processorCollectionMock = MockProvider.GetDBCollectionMock(processors);
            rams = new DbCollectionMockStorage();
            ramCollectionMock = MockProvider.GetDBCollectionMock(rams);
            aggregatorProviderMock = new Mock<IAggregatorProvider>(MockBehavior.Loose);
        }

        [Test]
        public void Get_test()
        {
            // arrange
            var maker = "Intel";
            var processorSpeed = 500;
            var ramSize = 2000;
            processors[0] = new Dictionary<string, object>
            {
                { "Maker", maker},
                { "Speed", processorSpeed}
            };
            rams[0] = new Dictionary<string, object>
            {
                { "Size", ramSize }
            };
            var processor = new Processor(0, processorCollectionMock.Object);
            var ram = new RAM(0, ramCollectionMock.Object);
            var computer = new Computer(aggregatorProviderMock.Object, processor, ram);

            // act and assert
            computer.Maker.Should().Be(maker);
            computer.ProcessorSpeed.Should().Be(processorSpeed);
            computer.RAMSize.Should().Be(ramSize);
        }

        [Test]
        public void Preload_test()
        {
            // arrange
            var maker = "Intel";
            var processorSpeed = 500;
            var ramSize = 2000;
            processors[0] = new Dictionary<string, object>
            {
                { "Maker", maker},
                { "Speed", processorSpeed}
            };
            rams[0] = new Dictionary<string, object>
            {
                { "Size", ramSize }
            };
            var processor = new Processor(0, processorCollectionMock.Object);
            var ram = new RAM(0, ramCollectionMock.Object);
            var computer = new Computer(aggregatorProviderMock.Object, processor, ram);

            // act
            computer.PreloadAgg(x => x.Maker,
                                x => x.ProcessorSpeed,
                                x => x.RAMSize);

            // assert
            var sth = computer.Maker;
            var sth2 = computer.Maker;
            var sth3 = computer.Maker;
            computer.Maker.Should().Be(maker);
            computer.ProcessorSpeed.Should().Be(processorSpeed);
            computer.RAMSize.Should().Be(ramSize);

            processorCollectionMock.Verify(x => 
                x.GetFields(It.IsAny<Id>(), It.IsAny<string[]>()),
                Times.Once());
            ramCollectionMock.Verify(x =>
                x.GetFields(It.IsAny<Id>(), It.IsAny<string[]>()),
                Times.Once());
        }

        [Test]
        public void Set_test()
        {
            // arrange
            var maker = "Intel";
            var processorSpeed = 500;
            var ramSize = 2000;
            processors[0] = new Dictionary<string, object>
            {
                { "Maker", maker},
                { "Speed", processorSpeed}
            };
            rams[0] = new Dictionary<string, object>
            {
                { "Size", ramSize }
            };
            var processor = new Processor(0, processorCollectionMock.Object);
            var ram = new RAM(0, ramCollectionMock.Object);
            var computer = new Computer(aggregatorProviderMock.Object, processor, ram);

            // act
            var newRamSize = 1;
            computer.RAMSize = newRamSize;
            computer.Commit();

            // assert
            rams[0]["Size"].Should().Be(newRamSize);
        }
    }
}
