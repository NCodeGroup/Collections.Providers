#region Copyright Preamble

// Copyright @ 2024 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

using Moq;

namespace NCode.Collections.Providers.Tests;

public class DefaultCollectionProviderFactoryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var factory = new DefaultCollectionProviderFactory();

        Assert.NotNull(factory);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Factory_ImplementsICollectionProviderFactory()
    {
        var factory = new DefaultCollectionProviderFactory();

        Assert.IsAssignableFrom<ICollectionProviderFactory>(factory);
    }

    #endregion

    #region Create Single DataSource Tests

    [Fact]
    public async Task Create_SingleDataSource_ReturnsNonNullProvider()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1", "item2"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Create_SingleDataSource_ReturnsCollectionProviderType()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1", "item2"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        Assert.IsType<CollectionProvider<string, IReadOnlyList<string>>>(provider);
    }

    [Fact]
    public async Task Create_SingleDataSource_WithOwnsTrue_DisposesDataSourceOnDispose()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(x => x.Dispose());

        var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object,
            owns: true
        );

        await provider.DisposeAsync();

        mockDataSource.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Create_SingleDataSource_WithOwnsFalse_DoesNotDisposeDataSourceOnDispose()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object,
            owns: false
        );

        await provider.DisposeAsync();

        mockDataSource.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task Create_SingleDataSource_DefaultOwnsIsTrue()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(x => x.Dispose());

        var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        await provider.DisposeAsync();

        mockDataSource.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Create_SingleDataSource_ProviderContainsCorrectItems()
    {
        var factory = new DefaultCollectionProviderFactory();
        string[] expectedItems = ["alpha", "beta", "gamma"];
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(expectedItems);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        Assert.Equal(expectedItems, provider.Collection);
    }

    [Fact]
    public async Task Create_SingleDataSource_UsesProvidedCollectionFactory()
    {
        var factory = new DefaultCollectionProviderFactory();
        var factoryInvoked = false;
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1", "item2"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items =>
            {
                factoryInvoked = true;
                return items.ToList();
            },
            mockDataSource.Object
        );

        _ = provider.Collection;

        Assert.True(factoryInvoked);
    }

    [Fact]
    public async Task Create_SingleDataSource_WithIntType_Works()
    {
        var factory = new DefaultCollectionProviderFactory();
        int[] expectedItems = [1, 2, 3, 4, 5];
        var mockDataSource = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(expectedItems);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<int, IReadOnlyList<int>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        Assert.Equal(expectedItems, provider.Collection);
    }

    [Fact]
    public async Task Create_SingleDataSource_WithComplexType_Works()
    {
        var factory = new DefaultCollectionProviderFactory();
        var person1 = new KeyValuePair<string, int>("Alice", 30);
        var person2 = new KeyValuePair<string, int>("Bob", 25);
        KeyValuePair<string, int>[] expectedItems = [person1, person2];
        var mockDataSource = new Mock<ICollectionDataSource<KeyValuePair<string, int>>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(expectedItems);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<KeyValuePair<string, int>, IReadOnlyList<KeyValuePair<string, int>>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        Assert.Equal(expectedItems, provider.Collection);
    }

    [Fact]
    public async Task Create_SingleDataSource_WithEmptyCollection_Works()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns([]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            mockDataSource.Object
        );

        Assert.Empty(provider.Collection);
    }

    #endregion

    #region Create Multiple DataSources Tests

    [Fact]
    public async Task Create_MultipleDataSources_ReturnsNonNullProvider()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["item2"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources
        );

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Create_MultipleDataSources_ReturnsCollectionProviderType()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["item2"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources
        );

        Assert.IsType<CollectionProvider<string, IReadOnlyList<string>>>(provider);
    }

    [Fact]
    public async Task Create_MultipleDataSources_AggregatesItems()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["alpha", "beta"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["gamma", "delta"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources
        );

        Assert.Equal(4, provider.Collection.Count);
        Assert.Contains("alpha", provider.Collection);
        Assert.Contains("beta", provider.Collection);
        Assert.Contains("gamma", provider.Collection);
        Assert.Contains("delta", provider.Collection);
    }

    [Fact]
    public async Task Create_MultipleDataSources_WithOwnsTrue_DisposesAllDataSourcesOnDispose()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource1.Setup(x => x.Dispose());
        var mockDataSource2 = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["item2"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource2.Setup(x => x.Dispose());
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources,
            owns: true
        );

        await provider.DisposeAsync();

        mockDataSource1.Verify(x => x.Dispose(), Times.Once);
        mockDataSource2.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Create_MultipleDataSources_WithOwnsFalse_DoesNotDisposeDataSourcesOnDispose()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["item2"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources,
            owns: false
        );

        await provider.DisposeAsync();

        mockDataSource1.Verify(x => x.Dispose(), Times.Never);
        mockDataSource2.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task Create_MultipleDataSources_DefaultOwnsIsFalse()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["item2"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources
        );

        await provider.DisposeAsync();

        mockDataSource1.Verify(x => x.Dispose(), Times.Never);
        mockDataSource2.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task Create_MultipleDataSources_WithEmptyCollection_ReturnsEmptyProvider()
    {
        var factory = new DefaultCollectionProviderFactory();
        ICollectionDataSource<string>[] dataSources = [];

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources
        );

        Assert.Empty(provider.Collection);
    }

    [Fact]
    public async Task Create_MultipleDataSources_WithSingleDataSource_Works()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["alpha", "beta"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        await using var provider = factory.Create<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSources
        );

        Assert.Equal(["alpha", "beta"], provider.Collection);
    }

    [Fact]
    public async Task Create_MultipleDataSources_WithThreeDataSources_AggregatesAllItems()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource1 = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns([1, 2]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns([3, 4]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource3 = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource3.Setup(x => x.Collection).Returns([5, 6]);
        mockDataSource3.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<int>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object, mockDataSource3.Object];

        await using var provider = factory.Create<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSources
        );

        Assert.Equal(6, provider.Collection.Count);
        Assert.Contains(1, provider.Collection);
        Assert.Contains(2, provider.Collection);
        Assert.Contains(3, provider.Collection);
        Assert.Contains(4, provider.Collection);
        Assert.Contains(5, provider.Collection);
        Assert.Contains(6, provider.Collection);
    }

    #endregion

    #region Collection Factory Tests

    [Fact]
    public async Task Create_WithArrayCollectionFactory_ReturnsArray()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["a", "b", "c"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, string[]>(
            items => items.ToArray(),
            mockDataSource.Object
        );

        Assert.IsType<string[]>(provider.Collection);
        Assert.Equal(["a", "b", "c"], provider.Collection);
    }

    [Fact]
    public async Task Create_WithHashSetCollectionFactory_ReturnsHashSet()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["a", "b", "c"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<string, HashSet<string>>(
            items => items.ToHashSet(),
            mockDataSource.Object
        );

        Assert.IsType<HashSet<string>>(provider.Collection);
        Assert.Equal(3, provider.Collection.Count);
    }

    [Fact]
    public async Task Create_WithCustomTransformation_TransformsItems()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<int, IReadOnlyList<int>>(
            items => items.Select(x => x * 2).ToList(),
            mockDataSource.Object
        );

        Assert.Equal([2, 4, 6], provider.Collection);
    }

    [Fact]
    public async Task Create_WithFilteringCollectionFactory_FiltersItems()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns([1, 2, 3, 4, 5, 6]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<int, IReadOnlyList<int>>(
            items => items.Where(x => x % 2 == 0).ToList(),
            mockDataSource.Object
        );

        Assert.Equal([2, 4, 6], provider.Collection);
    }

    [Fact]
    public async Task Create_WithOrderingCollectionFactory_OrdersItems()
    {
        var factory = new DefaultCollectionProviderFactory();
        var mockDataSource = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns([5, 2, 8, 1, 9]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var provider = factory.Create<int, IReadOnlyList<int>>(
            items => items.OrderBy(x => x).ToList(),
            mockDataSource.Object
        );

        Assert.Equal([1, 2, 5, 8, 9], provider.Collection);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Create_IsThreadSafe_MultipleCallsReturnIndependentProviders()
    {
        var factory = new DefaultCollectionProviderFactory();
        const int threadCount = 10;
        var providers = new ICollectionProvider<string, IReadOnlyList<string>>[threadCount];

        Parallel.For(0, threadCount, i =>
        {
            var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
            mockDataSource.Setup(x => x.Collection).Returns([$"item{i}"]);
            mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);

            providers[i] = factory.Create<string, IReadOnlyList<string>>(
                items => items.ToList(),
                mockDataSource.Object,
                owns: false
            );
        });

        for (var i = 0; i < threadCount; i++)
        {
            Assert.NotNull(providers[i]);
            Assert.Single(providers[i].Collection);
            Assert.Contains($"item{i}", providers[i].Collection);
            await providers[i].DisposeAsync();
        }
    }

    #endregion
}
