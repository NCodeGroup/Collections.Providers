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

using NCode.Collections.Providers.DataSources;

namespace NCode.Collections.Providers.Tests.DataSources;

public class StaticCollectionDataSourceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithCollection_CreatesDataSource()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.NotNull(dataSource);
    }

    [Fact]
    public void Constructor_MaterializesCollection_IntoList()
    {
        IEnumerable<int> lazyCollection = GetLazyCollection();

        using var dataSource = new StaticCollectionDataSource<int>(lazyCollection);

        Assert.IsType<List<int>>(dataSource.Collection);

        static IEnumerable<int> GetLazyCollection()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    [Fact]
    public void Constructor_WithEmptyCollection_CreatesDataSource()
    {
        int[] collection = [];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.NotNull(dataSource);
        Assert.Empty(dataSource.Collection);
    }

    [Fact]
    public void Constructor_OwnsDefaultsToTrue()
    {
        var disposableItem = new DisposableItem();
        DisposableItem[] collection = [disposableItem];

        var dataSource = new StaticCollectionDataSource<DisposableItem>(collection);
        dataSource.Dispose();

        Assert.True(disposableItem.IsDisposed);
    }

    [Fact]
    public void Constructor_WithOwnsFalse_DoesNotDisposeItems()
    {
        var disposableItem = new DisposableItem();
        DisposableItem[] collection = [disposableItem];

        var dataSource = new StaticCollectionDataSource<DisposableItem>(collection, owns: false);
        dataSource.Dispose();

        Assert.False(disposableItem.IsDisposed);
    }

    [Fact]
    public void Constructor_CapturesSnapshot_DoesNotReflectSourceChanges()
    {
        var sourceList = new List<int> { 1, 2, 3 };

        using var dataSource = new StaticCollectionDataSource<int>(sourceList);

        sourceList.Add(4);
        sourceList.Add(5);

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public void Collection_ReturnsCorrectItems()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    [Fact]
    public void Collection_ReturnsSameInstance_OnMultipleCalls()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var first = dataSource.Collection;
        var second = dataSource.Collection;

        Assert.Same(first, second);
    }

    [Fact]
    public void Collection_CanBeEnumeratedMultipleTimes()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var first = dataSource.Collection.ToList();
        var second = dataSource.Collection.ToList();
        var third = dataSource.Collection.ToList();

        Assert.Equal([1, 2, 3], first);
        Assert.Equal([1, 2, 3], second);
        Assert.Equal([1, 2, 3], third);
    }

    #endregion

    #region GetChangeToken Tests

    [Fact]
    public void GetChangeToken_ReturnsNullChangeToken()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var token = dataSource.GetChangeToken();

        Assert.Same(NullChangeToken.Singleton, token);
    }

    [Fact]
    public void GetChangeToken_ReturnsSameInstance_OnMultipleCalls()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var first = dataSource.GetChangeToken();
        var second = dataSource.GetChangeToken();

        Assert.Same(first, second);
    }

    [Fact]
    public void GetChangeToken_HasChangedIsFalse()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var token = dataSource.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    [Fact]
    public void GetChangeToken_ActiveChangeCallbacksIsFalse()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var token = dataSource.GetChangeToken();

        Assert.False(token.ActiveChangeCallbacks);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesOwnedItems()
    {
        var disposableItems = new[] { new DisposableItem(), new DisposableItem(), new DisposableItem() };

        var dataSource = new StaticCollectionDataSource<DisposableItem>(disposableItems, owns: true);
        dataSource.Dispose();

        Assert.All(disposableItems, item => Assert.True(item.IsDisposed));
    }

    [Fact]
    public void Dispose_DoesNotDisposeUnownedItems()
    {
        var disposableItems = new[] { new DisposableItem(), new DisposableItem(), new DisposableItem() };

        var dataSource = new StaticCollectionDataSource<DisposableItem>(disposableItems, owns: false);
        dataSource.Dispose();

        Assert.All(disposableItems, item => Assert.False(item.IsDisposed));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var disposeCount = 0;
        var disposableItem = new DisposableItem { OnDispose = () => disposeCount++ };
        DisposableItem[] collection = [disposableItem];

        var dataSource = new StaticCollectionDataSource<DisposableItem>(collection, owns: true);

        dataSource.Dispose();
        dataSource.Dispose();
        dataSource.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_DoesNotAffectNonDisposableItems()
    {
        int[] collection = [1, 2, 3];

        var dataSource = new StaticCollectionDataSource<int>(collection, owns: true);

        var exception = Record.Exception(() => dataSource.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CollectionStillAccessible_AfterDispose()
    {
        int[] collection = [1, 2, 3];

        var dataSource = new StaticCollectionDataSource<int>(collection, owns: true);
        dataSource.Dispose();

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void DataSource_ImplementsICollectionDataSource()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.IsAssignableFrom<ICollectionDataSource<int>>(dataSource);
    }

    [Fact]
    public void DataSource_ImplementsIDisposable()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.IsAssignableFrom<IDisposable>(dataSource);
    }

    [Fact]
    public void DataSource_DoesNotImplementIDisposableCollectionDataSource()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);
        ICollectionDataSource<int> abstractDataSource = dataSource;

        Assert.False(abstractDataSource is IDisposableCollectionDataSource<int>);
    }

    [Fact]
    public void DataSource_DoesNotImplementIAsyncDisposable()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);
        ICollectionDataSource<int> abstractDataSource = dataSource;

        Assert.False(abstractDataSource is IAsyncDisposable);
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void DataSource_WorksWithStringType()
    {
        string[] collection = ["hello", "world"];

        using var dataSource = new StaticCollectionDataSource<string>(collection);

        Assert.Equal(["hello", "world"], dataSource.Collection);
    }

    [Fact]
    public void DataSource_WorksWithComplexType()
    {
        var items = new[] { new TestItem(1, "one"), new TestItem(2, "two") };

        using var dataSource = new StaticCollectionDataSource<TestItem>(items);

        var result = dataSource.Collection.ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("two", result[1].Name);
    }

    [Fact]
    public void DataSource_WorksWithNullableReferenceType()
    {
        string?[] collection = ["hello", null, "world"];

        using var dataSource = new StaticCollectionDataSource<string?>(collection);

        var result = dataSource.Collection.ToList();

        Assert.Equal(3, result.Count);
        Assert.Null(result[1]);
    }

    [Fact]
    public void DataSource_WorksWithValueType()
    {
        int[] collection = [1, 2, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    [Fact]
    public void DataSource_WorksWithNullableValueType()
    {
        int?[] collection = [1, null, 3];

        using var dataSource = new StaticCollectionDataSource<int?>(collection);

        var result = dataSource.Collection.ToList();

        Assert.Equal(3, result.Count);
        Assert.Null(result[1]);
    }

    private record TestItem(int Id, string Name);

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Collection_IsThreadSafe_ForConcurrentAccess()
    {
        int[] collection = [1, 2, 3];
        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => dataSource.Collection.ToList()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal([1, 2, 3], r));
    }

    [Fact]
    public async Task GetChangeToken_IsThreadSafe_ForConcurrentAccess()
    {
        int[] collection = [1, 2, 3];
        using var dataSource = new StaticCollectionDataSource<int>(collection);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => dataSource.GetChangeToken()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var firstToken = results[0];
        Assert.All(results, r => Assert.Same(firstToken, r));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithSingleItem_CreatesDataSource()
    {
        int[] collection = [42];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.Single(dataSource.Collection);
        Assert.Equal(42, dataSource.Collection.First());
    }

    [Fact]
    public void Constructor_WithLargeCollection_CreatesDataSource()
    {
        var collection = Enumerable.Range(1, 10000).ToArray();

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.Equal(10000, dataSource.Collection.Count());
        Assert.Equal(1, dataSource.Collection.First());
        Assert.Equal(10000, dataSource.Collection.Last());
    }

    [Fact]
    public void Collection_PreservesOrder()
    {
        int[] collection = [5, 3, 1, 4, 2];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.Equal([5, 3, 1, 4, 2], dataSource.Collection);
    }

    [Fact]
    public void Collection_PreservesDuplicates()
    {
        int[] collection = [1, 2, 2, 3, 3, 3];

        using var dataSource = new StaticCollectionDataSource<int>(collection);

        Assert.Equal([1, 2, 2, 3, 3, 3], dataSource.Collection);
    }

    #endregion

    #region Test Helpers

    private sealed class DisposableItem : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public Action? OnDispose { get; init; }

        public void Dispose()
        {
            IsDisposed = true;
            OnDispose?.Invoke();
        }
    }

    #endregion
}
