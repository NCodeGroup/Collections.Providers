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

namespace NCode.Collections.Providers.Tests.DataSources;

public class CompositeCollectionDataSourceTests
{
    #region Test Helpers

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

    #endregion

    #region Constructor Tests

    [Fact]
    public async Task Constructor_WithDataSources_CreatesComposite()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource1, dataSource2]);

        Assert.NotNull(composite);
        dataSource1.Dispose();
        dataSource2.Dispose();
    }

    [Fact]
    public async Task Constructor_WithDataSourcesAndAdditionalChangeTokenProducers_CreatesComposite()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var additionalProducer = new TestChangeTokenProducer();

        await using var composite = new CompositeCollectionDataSource<int>(
            [dataSource],
            [additionalProducer]);

        Assert.NotNull(composite);
        dataSource.Dispose();
    }

    [Fact]
    public async Task Constructor_WithEmptyDataSources_CreatesComposite()
    {
        await using var composite = new CompositeCollectionDataSource<int>([]);

        Assert.NotNull(composite);
    }

    #endregion

    #region Owns Property Tests

    [Fact]
    public async Task Owns_DefaultsToFalse()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource]);

        Assert.False(composite.Owns);
        dataSource.Dispose();
    }

    [Fact]
    public async Task Owns_CanBeSetToTrue()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        Assert.True(composite.Owns);
    }

    #endregion

    #region CombineFunc Property Tests

    [Fact]
    public async Task CombineFunc_DefaultsConcatenation()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource1, dataSource2])
        {
            Owns = true
        };

        var collection = composite.Collection.ToList();

        Assert.Equal([1, 2, 3, 4, 5, 6], collection);
    }

    [Fact]
    public async Task CombineFunc_CanBeCustomized_ToDistinct()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([2, 3, 4]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource1, dataSource2])
        {
            Owns = true,
            CombineFunc = collections => collections.SelectMany(c => c).Distinct()
        };

        var collection = composite.Collection.ToList();

        Assert.Equal([1, 2, 3, 4], collection);
    }

    [Fact]
    public async Task CombineFunc_CanBeCustomized_ToSort()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([5, 3, 1]);
        var dataSource2 = new StaticCollectionDataSource<int>([6, 4, 2]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource1, dataSource2])
        {
            Owns = true,
            CombineFunc = collections => collections.SelectMany(c => c).OrderBy(x => x)
        };

        var collection = composite.Collection.ToList();

        Assert.Equal([1, 2, 3, 4, 5, 6], collection);
    }

    [Fact]
    public async Task CombineFunc_CanBeCustomized_ToFilter()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource1, dataSource2])
        {
            Owns = true,
            CombineFunc = collections => collections.SelectMany(c => c).Where(x => x % 2 == 0)
        };

        var collection = composite.Collection.ToList();

        Assert.Equal([2, 4, 6], collection);
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public async Task Collection_AggregatesMultipleDataSources()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([1, 2, 3]);
        var dataSource2 = new StaticCollectionDataSource<int>([4, 5, 6]);
        var dataSource3 = new StaticCollectionDataSource<int>([7, 8, 9]);

        await using var composite = new CompositeCollectionDataSource<int>(
            [dataSource1, dataSource2, dataSource3])
        {
            Owns = true
        };

        var collection = composite.Collection.ToList();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], collection);
    }

    [Fact]
    public async Task Collection_ReturnsEmptyCollection_WhenAllDataSourcesAreEmpty()
    {
        var dataSource1 = new StaticCollectionDataSource<int>([]);
        var dataSource2 = new StaticCollectionDataSource<int>([]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource1, dataSource2])
        {
            Owns = true
        };

        var collection = composite.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Collection_ReturnsEmptyCollection_WhenNoDataSources()
    {
        await using var composite = new CompositeCollectionDataSource<int>([]);

        var collection = composite.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Collection_LazilyInitializesCollection()
    {
        var accessCount = 0;
        var mockDataSource = new Mock<ICollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns(() =>
        {
            accessCount++;
            return [1, 2, 3];
        });
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);

        await using var composite = new CompositeCollectionDataSource<int>([mockDataSource.Object]);

        Assert.Equal(0, accessCount);

        _ = composite.Collection.ToList();

        Assert.Equal(1, accessCount);
    }

    [Fact]
    public async Task Collection_ThrowsObjectDisposedException_WhenDisposed()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        await composite.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => composite.Collection);
    }

    #endregion

    #region GetChangeToken Tests

    [Fact]
    public async Task GetChangeToken_ReturnsNonNullToken()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        var token = composite.GetChangeToken();

        Assert.NotNull(token);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsCachedTokenOnSubsequentCalls()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        var first = composite.GetChangeToken();
        var second = composite.GetChangeToken();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetChangeToken_HasChangedIsFalse_Initially()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        var token = composite.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    [Fact]
    public async Task GetChangeToken_ThrowsObjectDisposedException_WhenDisposed()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        await composite.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => composite.GetChangeToken());
    }

    #endregion

    #region Change Notification Tests

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);

        await using var composite = new CompositeCollectionDataSource<int>([observableDataSource]);

        var token = composite.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        var hasChangedBefore = token.HasChanged;
        observableCollection.Add(4);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(hasChangedBefore);
        Assert.True(token.HasChanged);
    }

    [Fact]
    public async Task Collection_RefreshesAutomatically_WhenDataSourceChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);

        await using var composite = new CompositeCollectionDataSource<int>([observableDataSource]);

        var initialCollection = composite.Collection.ToList();
        var token = composite.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        Assert.Equal([1, 2, 3], initialCollection);

        observableCollection.Add(4);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var updatedCollection = composite.Collection.ToList();

        Assert.Equal([1, 2, 3, 4], updatedCollection);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsNewToken_AfterChange()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);

        await using var composite = new CompositeCollectionDataSource<int>([observableDataSource]);

        var originalToken = composite.GetChangeToken();
        var tcs = new TaskCompletionSource();
        originalToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Add(4);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var newToken = composite.GetChangeToken();

        Assert.NotSame(originalToken, newToken);
        Assert.True(originalToken.HasChanged);
        Assert.False(newToken.HasChanged);
    }

    [Fact]
    public async Task AdditionalChangeTokenProducer_TriggersRefresh_WhenItChanges()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        var additionalProducer = new TestChangeTokenProducer();

        await using var composite = new CompositeCollectionDataSource<int>(
            [dataSource],
            [additionalProducer])
        {
            Owns = true
        };

        _ = composite.Collection;
        var originalToken = composite.GetChangeToken();
        var tcs = new TaskCompletionSource();
        originalToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        additionalProducer.TriggerChange();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(originalToken.HasChanged);
    }

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenAnyDataSourceChanges()
    {
        var observableCollection1 = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource1 = new ObservableCollectionDataSource<int>(observableCollection1);
        var observableCollection2 = new ObservableCollection<int>([4, 5, 6]);
        using var observableDataSource2 = new ObservableCollectionDataSource<int>(observableCollection2);

        await using var composite = new CompositeCollectionDataSource<int>(
            [observableDataSource1, observableDataSource2]);

        var token = composite.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection2.Add(7);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_DisposesOwnedDataSources()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(ds => ds.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var composite = new CompositeCollectionDataSource<int>([mockDataSource.Object])
        {
            Owns = true
        };

        await composite.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DoesNotDisposeUnownedDataSources()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);

        var composite = new CompositeCollectionDataSource<int>([mockDataSource.Object])
        {
            Owns = false
        };

        await composite.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var mockDataSource = new Mock<IAsyncDisposableCollectionDataSource<int>>();
        mockDataSource.Setup(ds => ds.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(ds => ds.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(ds => ds.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var composite = new CompositeCollectionDataSource<int>([mockDataSource.Object])
        {
            Owns = true
        };

        await composite.DisposeAsync();
        await composite.DisposeAsync();
        await composite.DisposeAsync();

        mockDataSource.Verify(ds => ds.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DisposesChangeTokenRegistrations()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);

        var composite = new CompositeCollectionDataSource<int>([observableDataSource]);
        _ = composite.GetChangeToken();

        await composite.DisposeAsync();

        observableCollection.Add(4);

        Assert.Throws<ObjectDisposedException>(() => composite.Collection);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public async Task Composite_ImplementsICollectionDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        Assert.IsAssignableFrom<ICollectionDataSource<int>>(composite);
    }

    [Fact]
    public async Task Composite_ImplementsIAsyncDisposableCollectionDataSource()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        Assert.IsAssignableFrom<IAsyncDisposableCollectionDataSource<int>>(composite);
    }

    [Fact]
    public async Task Composite_ImplementsIAsyncDisposable()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        Assert.IsAssignableFrom<IAsyncDisposable>(composite);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Collection_IsThreadSafe_ForConcurrentAccess()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => composite.Collection.ToList()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal([1, 2, 3], r));
    }

    [Fact]
    public async Task GetChangeToken_IsThreadSafe_ForConcurrentAccess()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);

        await using var composite = new CompositeCollectionDataSource<int>([dataSource])
        {
            Owns = true
        };

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => composite.GetChangeToken()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var firstToken = results[0];
        Assert.All(results, r => Assert.Same(firstToken, r));
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public async Task Composite_WorksWithStringType()
    {
        var dataSource1 = new StaticCollectionDataSource<string>(["hello"]);
        var dataSource2 = new StaticCollectionDataSource<string>(["world"]);

        await using var composite = new CompositeCollectionDataSource<string>([dataSource1, dataSource2])
        {
            Owns = true
        };

        var collection = composite.Collection.ToList();

        Assert.Equal(["hello", "world"], collection);
    }

    [Fact]
    public async Task Composite_WorksWithComplexType()
    {
        var items1 = new[] { new TestItem(1, "one") };
        var items2 = new[] { new TestItem(2, "two") };
        var dataSource1 = new StaticCollectionDataSource<TestItem>(items1);
        var dataSource2 = new StaticCollectionDataSource<TestItem>(items2);

        await using var composite = new CompositeCollectionDataSource<TestItem>([dataSource1, dataSource2])
        {
            Owns = true
        };

        var collection = composite.Collection.ToList();

        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0].Id);
        Assert.Equal("two", collection[1].Name);
    }

    private record TestItem(int Id, string Name);

    #endregion
}
