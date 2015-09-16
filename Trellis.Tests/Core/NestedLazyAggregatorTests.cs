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
    public class NestedLazyAggregatorTests
    {
        class ProcessorModel : LazyModel
        {
            public ProcessorModel(Id id, IDBCollection coll)
                : base(id, coll)
            { }
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
            public Id RamId
            {
                get { return PropertyGetter("RamId"); }
                set { PropertySetter("RamId", value); }
            }
        }

        class RAMModel : LazyModel
        {
            public RAMModel(Id id, IDBCollection coll)
                : base(id, coll)
            { }
            public int Size
            {
                get { return PropertyGetter("Size"); }
                set { PropertySetter("Size", value); }
            }
        }

        class RAM : LazyAggregator
        {
            static RAM()
            {
                UsingModel<RAM, RAMModel>();
            }
            public RAM(IAggregatorProvider aggProvider, RAMModel ramModel) 
                :base(aggProvider, ramModel)
            { }
            public int Size
            {
                get { return PropertyGetter<int>("Size"); }
                set { PropertySetter("Size", value); }
            }
        }

        class Computer : LazyAggregator
        {
            static Computer()
            {
                UsingModel<Computer, ProcessorModel>();

                Setup<Computer>()
                    .ForeignAggregator(x => x.RAM).IdFrom<ProcessorModel>(x => x.RamId)
                    .Field(x => x.ProcessorSpeed)
                        .From(x => x.M<ProcessorModel>().Speed)
                        .To<ProcessorModel, int>(x => x.Speed).With(x => x)
                        .Using<ProcessorModel>(x => x.Speed);
            }
            public Computer(IAggregatorProvider aggProvider, ProcessorModel processor)
                :base(aggProvider, processor)
            { }

            public RAM RAM
            {
                get { return PropertyGetter<RAM>("RAM"); }
                set { PropertySetter("RAM", value); }
            }
            public int ProcessorSpeed
            {
                get { return PropertyGetter<int>("ProcessorSpeed"); }
                set { PropertySetter("ProcessorSpeed", value); }
            }
            public string Maker
            {
                get { return PropertyGetter<string>("Maker"); }
                set { PropertySetter("Maker", value); }
            }
        }

        DBCollectionMockStorage processors;
        Mock<IDBCollection> processorCollectionMock;
        DBCollectionMockStorage rams;
        Mock<IDBCollection> ramCollectionMock;
        Mock<IAggregatorProvider> aggregatorProviderMock;

        [SetUp]
        public void SetUp()
        {
            processors = new DBCollectionMockStorage();
            processorCollectionMock = MockProvider.GetDBCollectionMock(processors);
            rams = new DBCollectionMockStorage();
            ramCollectionMock = MockProvider.GetDBCollectionMock(rams);
            aggregatorProviderMock = MockProvider.GetAggregatorProviderMock();
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
                { "Speed", processorSpeed},
                { "RamId", 0 }
            };
            rams[0] = new Dictionary<string, object>
            {
                { "Size", ramSize }
            };
            var aggregatorProvider = aggregatorProviderMock.Object;
            var processorModel = new ProcessorModel(0, processorCollectionMock.Object);
            var ramModel = new RAMModel(0, ramCollectionMock.Object);
            var computer = new Computer(aggregatorProvider, processorModel);
            var ram = new RAM(aggregatorProvider, ramModel);
            aggregatorProvider.Save(computer, ram);

            // act and assert
            computer.RAM.Size.Should().Be(ramSize);
            computer.ProcessorSpeed.Should().Be(processorSpeed);
            computer.Maker.Should().Be(maker);
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
                { "Speed", processorSpeed},
                { "RamId", 0 }
            };
            rams[0] = new Dictionary<string, object>
            {
                { "Size", ramSize }
            };
            var aggregatorProvider = aggregatorProviderMock.Object;
            var processorModel = new ProcessorModel(0, processorCollectionMock.Object);
            var ramModel = new RAMModel(0, ramCollectionMock.Object);
            var computer = new Computer(aggregatorProvider, processorModel);
            var ram = new RAM(aggregatorProvider, ramModel);
            aggregatorProvider.Save(computer, ram);

            // act
            var newMaker = "AMD";
            var newProcessorSpeed = 1000;
            var newRamSize = 3000;
            computer.RAM.Size = newRamSize;
            computer.ProcessorSpeed = newProcessorSpeed;
            computer.Maker = newMaker;
            computer.Commit();

            // assert
            processors[0]["Maker"].Should().Be(newMaker);
            processors[0]["Speed"].Should().Be(newProcessorSpeed);
            rams[0]["Size"].Should().Be(newRamSize);
            processorCollectionMock.Verify(x => 
                x.UpdateFields(It.IsAny<Id>(), It.IsAny<IDictionary<string, object>>()), 
                Times.Once);
            ramCollectionMock.Verify(x =>
                x.UpdateFields(It.IsAny<Id>(), It.IsAny<IDictionary<string, object>>()),
                Times.Once);
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
                { "Speed", processorSpeed},
                { "RamId", 0 }
            };
            rams[0] = new Dictionary<string, object>
            {
                { "Size", ramSize }
            };
            var aggregatorProvider = aggregatorProviderMock.Object;
            var processorModel = new ProcessorModel(0, processorCollectionMock.Object);
            var ramModel = new RAMModel(0, ramCollectionMock.Object);
            var computer = new Computer(aggregatorProvider, processorModel);
            var ram = new RAM(aggregatorProvider, ramModel);
            aggregatorProvider.Save(computer, ram);

            // act
            computer.PreloadAgg(x => x.Maker,
                                x => x.ProcessorSpeed,
                                x => x.RAM);

            // assert
            computer.RAM.Size.Should().Be(ramSize);
            computer.ProcessorSpeed.Should().Be(processorSpeed);
            computer.Maker.Should().Be(maker);
            processorCollectionMock.Verify(x =>
                x.GetFields(It.IsAny<Id>(), It.IsAny<string[]>()),
                Times.Once);
            processorCollectionMock.Verify(x =>
                x.GetModelField(It.IsAny<Id>(), It.IsAny<string>()),
                Times.Never);
            ramCollectionMock.Verify(x =>
                x.GetFields(It.IsAny<Id>(), It.IsAny<string[]>()),
                Times.Once);
            ramCollectionMock.Verify(x =>
                x.GetModelField(It.IsAny<Id>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}
