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
using Microsoft.Extensions.Primitives;
using Moq;
using NCode.Collections.Providers.DataSources;

namespace NCode.Collections.Providers.Tests;

public class BaseCollectionProviderTests
{
    #region Test Helpers

    private class TestCollectionProvider : BaseCollectionProvider<int, IReadOnlyList<int>>
    {
        public int CreateCollectionCallCount { get; private set; }

        public TestCollectionProvider(ICollectionDataSource<int> dataSource, bool owns = true)
            : base(dataSource, owns)
        {
        }

        public TestCollectionProvider(
            ICollectionDataSource<int> dataSource,
            IEnumerable<ISupportChangeToken> additionalChangeTokenProducers,
            bool owns = true)
            : base(dataSource, additionalChangeTokenProducers, owns)
        {
        }

        public TestCollectionProvider(IEnumerable<ICollectionDataSource<int>> dataSources, bool owns = false)
            : base(dataSources, owns)
        {
        }

        public TestCollectionProvider(
            IEnumerable<ICollectionDataSource<int>> dataSources,
            IEnumerable<ISupportChangeToken> additionalChangeTokenProducers,
            bool owns = false)
            : base(dataSources, additionalChangeTokenProducers, owns)
        {
        }

        protected override IReadOnlyList<int> CreateCollection(IEnumerable<int> items)
        {
            CreateCollectionCallCount++;
            return items.ToList();
        }
    }

    private class TestChangeTokenProducer : ISupportChangeToken
    {
        private CancellationTokenSource? _cancellationTokenSource;

        public IChangeToken GetChangeToken()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            return new CancellationChangeToken(_cancellationTokenSource.Token);
        }

        public void TriggerChange()
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    private static void ResetObservableCollection(ObservableCollection<int> collection, IEnumerable<int> newItems)
    {
        collection.Clear();
        foreach (var item in newItems)
        {
            collection.Add(item);
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public async Task Constructor_WithSingleDataSource_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new TestCollectionProvider(dataSource);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithSingleDataSourceAndOwnsTrue_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new TestCollectionProvider(dataSource, owns: true);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithSingleDataSourceAndOwnsFalse_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var provider = new TestCollectionProvider(dataSource, owns: false);

        Assert.NotNull(provider);
        dataSource.Dispose();
    }

    [Fact]
    public async Task Constructor_WithSingleDataSourceAndAdditionalChangeTokenProducers_CreatesProvider()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var additionalProducer = new TestChangeTokenProducer();

        await using var provider = new TestCollectionProvider(dataSource, [additionalProducer]);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithMultipleDataSources_CreatesProvider()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var provider = new TestCollectionProvider([dataSource1, dataSource2]);

        Assert.NotNull(provider);
        dataSource1.Dispose();
        dataSource2.Dispose();
    }

    [Fact]
    public async Task Constructor_WithMultipleDataSourcesAndOwnsTrue_CreatesProvider()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var provider = new TestCollectionProvider([dataSource1, dataSource2], owns: true);

        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Constructor_WithMultipleDataSourcesAndAdditionalChangeTokenProducers_CreatesProvider()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);
        var additionalProducer = new TestChangeTokenProducer();

        await using var provider = new TestCollectionProvider([dataSource1, dataSource2], [additionalProducer]);

        Assert.NotNull(provider);
        dataSource1.Dispose();
        dataSource2.Dispose();
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public async Task Collection_ReturnsItemsFromDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var collection = provider.Collection;

        Assert.Equal([1, 2, 3], collection);
    }

    [Fact]
    public async Task Collection_LazilyInitializesCollection()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        Assert.Equal(0, provider.CreateCollectionCallCount);

        _ = provider.Collection;

        Assert.Equal(1, provider.CreateCollectionCallCount);
    }

    [Fact]
    public async Task Collection_ReturnsCachedCollectionOnSubsequentAccess()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var first = provider.Collection;
        var second = provider.Collection;

        Assert.Same(first, second);
        Assert.Equal(1, provider.CreateCollectionCallCount);
    }

    [Fact]
    public async Task Collection_AggregatesMultipleDataSources()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);
        await using var provider = new TestCollectionProvider([dataSource1, dataSource2], owns: true);

        var collection = provider.Collection;

        Assert.Equal([1, 2, 3, 4, 5, 6], collection);
    }

    [Fact]
    public async Task Collection_ThrowsObjectDisposedException_WhenDisposed()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var provider = new TestCollectionProvider(dataSource);

        await provider.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => provider.Collection);
    }

    #endregion

    #region ICollectionDataSource Implementation Tests

    [Fact]
    public async Task ICollectionDataSource_Collection_ReturnsItemsFromDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);
        ICollectionDataSource<int> dataSourceInterface = provider;

        var collection = dataSourceInterface.Collection;

        Assert.Equal([1, 2, 3], collection);
    }

    #endregion

    #region GetChangeToken Tests

    [Fact]
    public async Task GetChangeToken_ReturnsNonNullToken()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var token = provider.GetChangeToken();

        Assert.NotNull(token);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsCachedTokenOnSubsequentCalls()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var first = provider.GetChangeToken();
        var second = provider.GetChangeToken();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetChangeToken_HasChangedIsFalse_Initially()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var token = provider.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    [Fact]
    public async Task GetChangeToken_ActiveChangeCallbacksIsTrue()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var token = provider.GetChangeToken();

        Assert.True(token.ActiveChangeCallbacks);
    }

    [Fact]
    public async Task GetChangeToken_ThrowsObjectDisposedException_WhenDisposed()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var provider = new TestCollectionProvider(dataSource);

        await provider.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => provider.GetChangeToken());
    }

    #endregion

    #region Change Notification Tests

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        var token = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        var hasChangedBefore = token.HasChanged;
        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(hasChangedBefore);
        Assert.True(token.HasChanged);
    }

    [Fact]
    public async Task ChangeToken_CallbackIsInvoked_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        var token = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(tcs.Task.IsCompleted);
    }

    [Fact]
    public async Task Collection_RefreshesAutomatically_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        var initialCollection = provider.Collection;
        var token = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        Assert.Equal([1, 2, 3], initialCollection);

        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var updatedCollection = provider.Collection;

        Assert.Equal([4, 5, 6], updatedCollection);
        Assert.NotSame(initialCollection, updatedCollection);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsNewToken_AfterChange()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        var originalToken = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        originalToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var newToken = provider.GetChangeToken();

        Assert.NotSame(originalToken, newToken);
        Assert.True(originalToken.HasChanged);
        Assert.False(newToken.HasChanged);
    }

    [Fact]
    public async Task CreateCollection_CalledAgain_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        _ = provider.Collection;
        var token = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        Assert.Equal(1, provider.CreateCollectionCallCount);

        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(provider.CreateCollectionCallCount > 1);
    }

    [Fact]
    public async Task AdditionalChangeTokenProducer_TriggersRefresh_WhenItChanges()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var additionalProducer = new TestChangeTokenProducer();
        await using var provider = new TestCollectionProvider(dataSource, [additionalProducer]);
        _ = provider.Collection;
        var originalToken = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        originalToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        Assert.Equal(1, provider.CreateCollectionCallCount);

        additionalProducer.TriggerChange();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(originalToken.HasChanged);
        Assert.Equal(2, provider.CreateCollectionCallCount);
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
        var provider = new TestCollectionProvider(mockDataSource.Object, owns: true);

        await provider.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DoesNotDisposeUnownedDataSource()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var provider = new TestCollectionProvider(mockDataSource.Object, owns: false);

        await provider.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_DisposesOwnedDataSourcesInComposite()
    {
        var mockDataSource1 = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource1.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource1.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource1.Setup(ds => ds.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var mockDataSource2 = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource2.Setup(ds => ds.Collection).Returns([4, 5, 6]);
        mockDataSource2.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource2.Setup(ds => ds.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var provider = new TestCollectionProvider([mockDataSource1.Object, mockDataSource2.Object], owns: true);

        await provider.DisposeAsync();

        mockDataSource1.Verify(ds => ds.DisposeAsync(), Times.Once);
        mockDataSource2.Verify(ds => ds.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(ds => ds.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var provider = new TestCollectionProvider(mockDataSource.Object, owns: true);

        await provider.DisposeAsync();
        await provider.DisposeAsync();
        await provider.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DisposesChangeTokenRegistrations()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        var provider = new TestCollectionProvider(observableDataSource, owns: false);
        _ = provider.GetChangeToken();

        await provider.DisposeAsync();

        ResetObservableCollection(observableCollection, [4, 5, 6]);

        Assert.Throws<ObjectDisposedException>(() => provider.Collection);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Collection_IsThreadSafe_ForConcurrentAccess()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() => provider.Collection)).ToArray();
        var results = await Task.WhenAll(tasks);

        var firstCollection = results[0];
        Assert.All(results, r => Assert.Same(firstCollection, r));
        Assert.Equal(1, provider.CreateCollectionCallCount);
    }

    [Fact]
    public async Task GetChangeToken_IsThreadSafe_ForConcurrentAccess()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new TestCollectionProvider(dataSource);

        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() => provider.GetChangeToken())).ToArray();
        var results = await Task.WhenAll(tasks);

        var firstToken = results[0];
        Assert.All(results, r => Assert.Same(firstToken, r));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task Collection_HandlesEmptyDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([]);
        await using var provider = new TestCollectionProvider(dataSource);

        var collection = provider.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Collection_HandlesMultipleEmptyDataSources()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([]);
        var dataSource2 = new StaticCollectionDataSource<int>([]);
        await using var provider = new TestCollectionProvider([dataSource1, dataSource2], owns: true);

        var collection = provider.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Collection_NotRefreshed_WhenOnlyChangeTokenWasAccessed()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        var token = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        Assert.Equal(0, provider.CreateCollectionCallCount);

        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(0, provider.CreateCollectionCallCount);

        var collection = provider.Collection;

        Assert.Equal([4, 5, 6], collection);
        Assert.Equal(1, provider.CreateCollectionCallCount);
    }

    [Fact]
    public async Task ChangeToken_NotRefreshed_WhenOnlyCollectionWasAccessedBeforeChange()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new TestCollectionProvider(observableDataSource, owns: false);
        _ = provider.Collection;
        var initialToken = provider.GetChangeToken();
        var tcs = new TaskCompletionSource();
        initialToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        ResetObservableCollection(observableCollection, [4, 5, 6]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var token = provider.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    #endregion
}
