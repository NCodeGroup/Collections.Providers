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

namespace NCode.Collections.Providers.Tests.DataSources;

public class ObservableCollectionDataSourceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithObservableCollection_CreatesDataSource()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.NotNull(dataSource);
    }

    [Fact]
    public void Constructor_OwnsDefaultsToTrue()
    {
        var disposableItem = new DisposableItem();
        var observableCollection = new ObservableCollection<DisposableItem>([disposableItem]);

        var dataSource = new ObservableCollectionDataSource<DisposableItem>(observableCollection);
        dataSource.Dispose();

        Assert.True(disposableItem.IsDisposed);
    }

    [Fact]
    public void Constructor_WithOwnsFalse_DoesNotDisposeItems()
    {
        var disposableItem = new DisposableItem();
        var observableCollection = new ObservableCollection<DisposableItem>([disposableItem]);

        var dataSource = new ObservableCollectionDataSource<DisposableItem>(observableCollection, owns: false);
        dataSource.Dispose();

        Assert.False(disposableItem.IsDisposed);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_CreatesDataSource()
    {
        var observableCollection = new ObservableCollection<int>();

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.NotNull(dataSource);
        Assert.Empty(dataSource.Collection);
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public void Collection_ReturnsUnderlyingObservableCollection()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.Same(observableCollection, dataSource.Collection);
    }

    [Fact]
    public void Collection_ReturnsCorrectItems()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    [Fact]
    public void Collection_ReflectsChangesToUnderlyingCollection()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        observableCollection.Add(4);

        Assert.Equal([1, 2, 3, 4], dataSource.Collection);
    }

    [Fact]
    public void Collection_ThrowsObjectDisposedException_WhenDisposed()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        dataSource.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dataSource.Collection);
    }

    #endregion

    #region GetChangeToken Tests

    [Fact]
    public void GetChangeToken_ReturnsNonNullToken()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();

        Assert.NotNull(token);
    }

    [Fact]
    public void GetChangeToken_ReturnsCachedTokenOnSubsequentCalls()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var first = dataSource.GetChangeToken();
        var second = dataSource.GetChangeToken();

        Assert.Same(first, second);
    }

    [Fact]
    public void GetChangeToken_HasChangedIsFalse_Initially()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    [Fact]
    public void GetChangeToken_ThrowsObjectDisposedException_WhenDisposed()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        dataSource.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dataSource.GetChangeToken());
    }

    #endregion

    #region Change Notification Tests - Add

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenItemAdded()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        var hasChangedBefore = token.HasChanged;
        observableCollection.Add(4);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(hasChangedBefore);
        Assert.True(token.HasChanged);
    }

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenItemInserted()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Insert(1, 10);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
        Assert.Equal([1, 10, 2, 3], dataSource.Collection);
    }

    #endregion

    #region Change Notification Tests - Remove

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenItemRemoved()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Remove(2);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
        Assert.Equal([1, 3], dataSource.Collection);
    }

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenItemRemovedAt()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.RemoveAt(0);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
        Assert.Equal([2, 3], dataSource.Collection);
    }

    #endregion

    #region Change Notification Tests - Replace

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenItemReplaced()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection[1] = 20;
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
        Assert.Equal([1, 20, 3], dataSource.Collection);
    }

    #endregion

    #region Change Notification Tests - Move

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenItemMoved()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Move(0, 2);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
        Assert.Equal([2, 3, 1], dataSource.Collection);
    }

    #endregion

    #region Change Notification Tests - Clear

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenCollectionCleared()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Clear();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
        Assert.Empty(dataSource.Collection);
    }

    #endregion

    #region Change Token Refresh Tests

    [Fact]
    public async Task GetChangeToken_ReturnsNewToken_AfterChange()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var originalToken = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        originalToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Add(4);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var newToken = dataSource.GetChangeToken();

        Assert.NotSame(originalToken, newToken);
        Assert.True(originalToken.HasChanged);
        Assert.False(newToken.HasChanged);
    }

    [Fact]
    public async Task ChangeToken_CanBeSubscribedMultipleTimes()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var callbackCount = 0;

        for (var i = 0; i < 3; i++)
        {
            var token = dataSource.GetChangeToken();
            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ =>
            {
                Interlocked.Increment(ref callbackCount);
                tcs.TrySetResult();
            }, null);

            observableCollection.Add(i + 10);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(3, callbackCount);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesOwnedItems()
    {
        var disposableItems = new[] { new DisposableItem(), new DisposableItem(), new DisposableItem() };
        var observableCollection = new ObservableCollection<DisposableItem>(disposableItems);

        var dataSource = new ObservableCollectionDataSource<DisposableItem>(observableCollection, owns: true);
        dataSource.Dispose();

        Assert.All(disposableItems, item => Assert.True(item.IsDisposed));
    }

    [Fact]
    public void Dispose_DoesNotDisposeUnownedItems()
    {
        var disposableItems = new[] { new DisposableItem(), new DisposableItem(), new DisposableItem() };
        var observableCollection = new ObservableCollection<DisposableItem>(disposableItems);

        var dataSource = new ObservableCollectionDataSource<DisposableItem>(observableCollection, owns: false);
        dataSource.Dispose();

        Assert.All(disposableItems, item => Assert.False(item.IsDisposed));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var disposeCount = 0;
        var disposableItem = new DisposableItem { OnDispose = () => disposeCount++ };
        var observableCollection = new ObservableCollection<DisposableItem>([disposableItem]);

        var dataSource = new ObservableCollectionDataSource<DisposableItem>(observableCollection, owns: true);

        dataSource.Dispose();
        dataSource.Dispose();
        dataSource.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCollectionChangedEvent()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();
        dataSource.Dispose();

        observableCollection.Add(4);

        Assert.False(token.HasChanged);
    }

    [Fact]
    public void Dispose_DisposesChangeTokenSource()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        _ = dataSource.GetChangeToken();
        dataSource.Dispose();

        Assert.Throws<ObjectDisposedException>(() => dataSource.GetChangeToken());
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void DataSource_ImplementsICollectionDataSource()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.IsAssignableFrom<ICollectionDataSource<int>>(dataSource);
    }

    [Fact]
    public void DataSource_ImplementsIDisposableCollectionDataSource()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.IsAssignableFrom<IDisposableCollectionDataSource<int>>(dataSource);
    }

    [Fact]
    public void DataSource_ImplementsIDisposable()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        Assert.IsAssignableFrom<IDisposable>(dataSource);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Collection_IsThreadSafe_ForConcurrentAccess()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => dataSource.Collection.ToList()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal([1, 2, 3], r));
    }

    [Fact]
    public async Task GetChangeToken_IsThreadSafe_ForConcurrentAccess()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => dataSource.GetChangeToken()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var firstToken = results[0];
        Assert.All(results, r => Assert.Same(firstToken, r));
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void DataSource_WorksWithStringType()
    {
        var observableCollection = new ObservableCollection<string>(["hello", "world"]);

        using var dataSource = new ObservableCollectionDataSource<string>(observableCollection);

        Assert.Equal(["hello", "world"], dataSource.Collection);
    }

    [Fact]
    public void DataSource_WorksWithComplexType()
    {
        var items = new[] { new TestItem(1, "one"), new TestItem(2, "two") };
        var observableCollection = new ObservableCollection<TestItem>(items);

        using var dataSource = new ObservableCollectionDataSource<TestItem>(observableCollection);

        var collection = dataSource.Collection.ToList();

        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0].Id);
        Assert.Equal("two", collection[1].Name);
    }

    [Fact]
    public void DataSource_WorksWithNullableReferenceType()
    {
        var observableCollection = new ObservableCollection<string?>(["hello", null, "world"]);

        using var dataSource = new ObservableCollectionDataSource<string?>(observableCollection);

        var collection = dataSource.Collection.ToList();

        Assert.Equal(3, collection.Count);
        Assert.Null(collection[1]);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ChangeNotification_DoesNotFire_WhenDisposed()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var callbackFired = false;
        var token = dataSource.GetChangeToken();
        token.RegisterChangeCallback(_ => callbackFired = true, null);

        dataSource.Dispose();

        observableCollection.Add(4);
        await Task.Delay(50);

        Assert.False(callbackFired);
    }

    [Fact]
    public async Task ChangeNotification_WorksWithMultipleRapidChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var changeCount = 0;

        for (var i = 0; i < 5; i++)
        {
            var token = dataSource.GetChangeToken();
            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ =>
            {
                Interlocked.Increment(ref changeCount);
                tcs.TrySetResult();
            }, null);

            observableCollection.Add(i + 10);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(5, changeCount);
        Assert.Equal([1, 2, 3, 10, 11, 12, 13, 14], dataSource.Collection);
    }

    [Fact]
    public void ChangeToken_DoesNotSignal_WhenNoChangesMade()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var token = dataSource.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    [Fact]
    public void Collection_IsMutable_CanBeModifiedDirectly()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var dataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var collection = (ObservableCollection<int>)dataSource.Collection;
        collection.Add(4);

        Assert.Equal([1, 2, 3, 4], observableCollection);
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

    private sealed record TestItem(int Id, string Name);

    #endregion
}
