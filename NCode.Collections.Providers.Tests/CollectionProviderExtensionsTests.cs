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
using NCode.Collections.Providers.DataSources;

namespace NCode.Collections.Providers.Tests;

public class CollectionProviderExtensionsTests
{
    #region AsDataSource Tests

    [Fact]
    public async Task AsDataSource_ReturnsNonNullDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.NotNull(adaptedDataSource);
    }

    [Fact]
    public async Task AsDataSource_ReturnsICollectionDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.IsAssignableFrom<ICollectionDataSource<int>>(adaptedDataSource);
    }

    [Fact]
    public async Task AsDataSource_Collection_ReturnsProviderCollection()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.Equal([1, 2, 3], adaptedDataSource.Collection);
    }

    [Fact]
    public async Task AsDataSource_GetChangeToken_ReturnsProviderChangeToken()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();
        var providerToken = provider.GetChangeToken();
        var adaptedToken = adaptedDataSource.GetChangeToken();

        Assert.Same(providerToken, adaptedToken);
    }

    [Fact]
    public async Task AsDataSource_Collection_ReflectsProviderChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            observableDataSource,
            owns: false);

        var adaptedDataSource = provider.AsDataSource();
        var initialCollection = adaptedDataSource.Collection.ToList();
        var token = adaptedDataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Clear();
        observableCollection.Add(4);
        observableCollection.Add(5);
        observableCollection.Add(6);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var updatedCollection = adaptedDataSource.Collection.ToList();

        Assert.Equal([1, 2, 3], initialCollection);
        Assert.Equal([4, 5, 6], updatedCollection);
    }

    [Fact]
    public async Task AsDataSource_DoesNotImplementIDisposable()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.False(adaptedDataSource is IDisposable);
    }

    [Fact]
    public async Task AsDataSource_DoesNotImplementIAsyncDisposable()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.False(adaptedDataSource is IAsyncDisposable);
    }

    #endregion

    #region Hierarchical Composition Tests

    [Fact]
    public async Task AsDataSource_CanBeUsedAsSourceForAnotherProvider()
    {
        var originalDataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var firstProvider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.Select(x => x * 2),
            originalDataSource);

        var adaptedDataSource = firstProvider.AsDataSource();
        await using var secondProvider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.Select(x => x + 1).ToList(),
            adaptedDataSource,
            owns: false);

        var result = secondProvider.Collection;

        Assert.Equal([3, 5, 7], result);
    }

    [Fact]
    public async Task AsDataSource_MultipleAdaptations_ShareSameUnderlyingProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource1 = provider.AsDataSource();
        var adaptedDataSource2 = provider.AsDataSource();

        Assert.Equal(adaptedDataSource1.Collection, adaptedDataSource2.Collection);
        Assert.Same(adaptedDataSource1.GetChangeToken(), adaptedDataSource2.GetChangeToken());
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public async Task AsDataSource_WorksWithStringType()
    {
        var dataSource = new StaticCollectionDataSource<string>(["hello", "world"]);
        await using var provider = new CollectionProvider<string, IEnumerable<string>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.Equal(["hello", "world"], adaptedDataSource.Collection);
    }

    [Fact]
    public async Task AsDataSource_WorksWithComplexType()
    {
        var items = new[]
        {
            new TestItem(1, "one"),
            new TestItem(2, "two")
        };
        var dataSource = new StaticCollectionDataSource<TestItem>(items);
        await using var provider = new CollectionProvider<TestItem, IEnumerable<TestItem>>(
            i => i.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();
        var collection = adaptedDataSource.Collection.ToList();

        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0].Id);
        Assert.Equal("two", collection[1].Name);
    }

    private record TestItem(int Id, string Name);

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task AsDataSource_HandlesEmptyCollection()
    {
        var dataSource = new StaticCollectionDataSource<int>([]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();

        Assert.Empty(adaptedDataSource.Collection);
    }

    [Fact]
    public async Task AsDataSource_GetChangeToken_HasChangedIsFalseInitially()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);

        var adaptedDataSource = provider.AsDataSource();
        var token = adaptedDataSource.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    #endregion
}
