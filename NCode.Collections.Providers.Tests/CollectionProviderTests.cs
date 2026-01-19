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

using System.Collections.ObjectModel;
using Moq;
using NCode.Collections.Providers.DataSources;

namespace NCode.Collections.Providers.Tests;

public class CollectionProviderTests
{
    #region Constructor Tests - Single Data Source

    [Fact]
    public async Task Constructor_WithSingleDataSource_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithSingleDataSourceAndOwnsTrue_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource,
            owns: true);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithSingleDataSourceAndOwnsFalse_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource,
            owns: false);

        Assert.NotNull(provider);
        dataSource.Dispose();
    }

    #endregion

    #region Constructor Tests - Multiple Data Sources

    [Fact]
    public async Task Constructor_WithMultipleDataSources_CreatesProvider()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            [dataSource1, dataSource2]);

        Assert.NotNull(provider);
        dataSource1.Dispose();
        dataSource2.Dispose();
    }

    [Fact]
    public async Task Constructor_WithMultipleDataSourcesAndOwnsTrue_CreatesProvider()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            [dataSource1, dataSource2],
            owns: true);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithMultipleDataSourcesAndOwnsFalse_CreatesProvider()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            [dataSource1, dataSource2],
            owns: false);

        Assert.NotNull(provider);
        dataSource1.Dispose();
        dataSource2.Dispose();
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public async Task Collection_UsesFactoryDelegate_ToCreateCollection()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var factoryCalled = false;

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items =>
            {
                factoryCalled = true;
                return items.ToList();
            },
            dataSource);

        _ = provider.Collection;

        Assert.True(factoryCalled);
    }

    [Fact]
    public async Task Collection_ReturnsItemsFromDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal([1, 2, 3], collection);
    }

    [Fact]
    public async Task Collection_AggregatesMultipleDataSources()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            [dataSource1, dataSource2],
            owns: true);

        var collection = provider.Collection;

        Assert.Equal([1, 2, 3, 4, 5, 6], collection);
    }

    [Fact]
    public async Task Collection_FactoryCanTransformItems()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.Select(x => x * 2).ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal([2, 4, 6], collection);
    }

    [Fact]
    public async Task Collection_FactoryCanFilterItems()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3, 4, 5, 6]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.Where(x => x % 2 == 0).ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal([2, 4, 6], collection);
    }

    [Fact]
    public async Task Collection_FactoryCanSortItems()
    {
        var dataSource = new StaticCollectionDataSource<int>([3, 1, 4, 1, 5, 9, 2, 6]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.OrderBy(x => x).ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal([1, 1, 2, 3, 4, 5, 6, 9], collection);
    }

    [Fact]
    public async Task Collection_FactoryCanCreateDifferentCollectionTypes()
    {
        var dataSource = new StaticCollectionDataSource<string>(["a", "b", "c"]);

        await using var provider = new CollectionProvider<string, HashSet<string>>(
            items => new HashSet<string>(items),
            dataSource);

        var collection = provider.Collection;

        Assert.IsType<HashSet<string>>(collection);
        Assert.Contains("a", collection);
        Assert.Contains("b", collection);
        Assert.Contains("c", collection);
    }

    #endregion

    #region Change Notification Tests

    [Fact]
    public async Task Collection_RefreshesAutomatically_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            observableDataSource,
            owns: false);

        var initialCollection = provider.Collection;
        var token = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        Assert.Equal([1, 2, 3], initialCollection);

        observableCollection.Clear();
        observableCollection.Add(4);
        observableCollection.Add(5);
        observableCollection.Add(6);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var updatedCollection = provider.Collection;

        Assert.Equal([4, 5, 6], updatedCollection);
        Assert.NotSame(initialCollection, updatedCollection);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsValidToken()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource);

        var token = provider.GetChangeToken();

        Assert.NotNull(token);
        Assert.False(token.HasChanged);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_DisposesOwnedDataSource()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(ds => ds.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            mockDataSource.Object,
            owns: true);

        await provider.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DoesNotDisposeUnownedDataSource()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);

        var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            mockDataSource.Object,
            owns: false);

        await provider.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Collection_ThrowsObjectDisposedException_WhenDisposed()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource);

        await provider.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => provider.Collection);
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public async Task Collection_WorksWithStringType()
    {
        var dataSource = new StaticCollectionDataSource<string>(["hello", "world"]);

        await using var provider = new CollectionProvider<string, IReadOnlyList<string>>(
            items => items.ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal(["hello", "world"], collection);
    }

    [Fact]
    public async Task Collection_WorksWithComplexType()
    {
        var items = new[]
        {
            new TestItem(1, "one"),
            new TestItem(2, "two"),
            new TestItem(3, "three")
        };
        var dataSource = new StaticCollectionDataSource<TestItem>(items);

        await using var provider = new CollectionProvider<TestItem, IReadOnlyList<TestItem>>(
            i => i.ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal(3, collection.Count);
        Assert.Equal(1, collection[0].Id);
        Assert.Equal("two", collection[1].Name);
    }

    [Fact]
    public async Task Collection_WorksWithArrayCollectionType()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, int[]>(
            items => items.ToArray(),
            dataSource);

        var collection = provider.Collection;

        Assert.IsType<int[]>(collection);
        Assert.Equal([1, 2, 3], collection);
    }

    [Fact]
    public async Task Collection_WorksWithReadOnlyCollectionType()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new CollectionProvider<int, ReadOnlyCollection<int>>(
            items => new ReadOnlyCollection<int>(items.ToList()),
            dataSource);

        var collection = provider.Collection;

        Assert.IsType<ReadOnlyCollection<int>>(collection);
        Assert.Equal([1, 2, 3], collection);
    }

    private record TestItem(int Id, string Name);

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task Collection_HandlesEmptyDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Collection_HandlesEmptyMultipleDataSources()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([]);
        var dataSource2 = new StaticCollectionDataSource<int>([]);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            [dataSource1, dataSource2],
            owns: true);

        var collection = provider.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Collection_HandlesLargeDataSet()
    {
        var largeData = Enumerable.Range(1, 10000).ToList();
        var dataSource = new StaticCollectionDataSource<int>(largeData);

        await using var provider = new CollectionProvider<int, IReadOnlyList<int>>(
            items => items.ToList(),
            dataSource);

        var collection = provider.Collection;

        Assert.Equal(10000, collection.Count);
        Assert.Equal(1, collection[0]);
        Assert.Equal(10000, collection[9999]);
    }

    #endregion
}
