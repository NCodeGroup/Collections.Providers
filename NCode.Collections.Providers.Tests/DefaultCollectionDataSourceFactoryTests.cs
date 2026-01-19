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
using NCode.Collections.Providers.PeriodicPolling;

namespace NCode.Collections.Providers.Tests;

public class DefaultCollectionDataSourceFactoryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var factory = new DefaultCollectionDataSourceFactory();

        Assert.NotNull(factory);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Factory_ImplementsICollectionDataSourceFactory()
    {
        var factory = new DefaultCollectionDataSourceFactory();

        Assert.IsAssignableFrom<ICollectionDataSourceFactory>(factory);
    }

    #endregion

    #region CreateComposite Tests

    [Fact]
    public async Task CreateComposite_ReturnsNonNullDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1", "item2"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        await using var result = factory.CreateComposite(dataSources);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateComposite_ReturnsCompositeCollectionDataSourceType()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        await using var result = factory.CreateComposite(dataSources);

        Assert.IsType<CompositeCollectionDataSource<string>>(result);
    }

    [Fact]
    public async Task CreateComposite_ReturnsIAsyncDisposableCollectionDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        await using var result = factory.CreateComposite(dataSources);

        Assert.IsAssignableFrom<IAsyncDisposableCollectionDataSource<string>>(result);
    }

    [Fact]
    public async Task CreateComposite_AggregatesItemsFromMultipleDataSources()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource1 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource1.Setup(x => x.Collection).Returns(["alpha", "beta"]);
        mockDataSource1.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var mockDataSource2 = new Mock<ICollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource2.Setup(x => x.Collection).Returns(["gamma", "delta"]);
        mockDataSource2.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource1.Object, mockDataSource2.Object];

        await using var result = factory.CreateComposite(dataSources);

        var items = result.Collection.ToList();
        Assert.Equal(4, items.Count);
        Assert.Contains("alpha", items);
        Assert.Contains("beta", items);
        Assert.Contains("gamma", items);
        Assert.Contains("delta", items);
    }

    [Fact]
    public async Task CreateComposite_WithEmptyDataSources_ReturnsEmptyCollection()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        ICollectionDataSource<string>[] dataSources = [];

        await using var result = factory.CreateComposite(dataSources);

        Assert.Empty(result.Collection);
    }

    [Fact]
    public async Task CreateComposite_DefaultOwnsFalse_DoesNotDisposeDataSources()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        var result = factory.CreateComposite(dataSources);

        await result.DisposeAsync();

        mockDataSource.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task CreateComposite_WithOwnsFalse_DoesNotDisposeDataSources()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        var result = factory.CreateComposite(dataSources, owns: false);

        await result.DisposeAsync();

        mockDataSource.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task CreateComposite_WithOwnsTrue_DisposesDataSources()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<IDisposableCollectionDataSource<string>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns(["item1"]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        mockDataSource.Setup(x => x.Dispose());
        ICollectionDataSource<string>[] dataSources = [mockDataSource.Object];

        var result = factory.CreateComposite(dataSources, owns: true);

        await result.DisposeAsync();

        mockDataSource.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CreateComposite_WithIntType_Works()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var mockDataSource = new Mock<ICollectionDataSource<int>>(MockBehavior.Strict);
        mockDataSource.Setup(x => x.Collection).Returns([1, 2, 3]);
        mockDataSource.Setup(x => x.GetChangeToken()).Returns(NullChangeToken.Singleton);
        ICollectionDataSource<int>[] dataSources = [mockDataSource.Object];

        await using var result = factory.CreateComposite(dataSources);

        Assert.Equal([1, 2, 3], result.Collection);
    }

    #endregion

    #region CreateStatic Tests

    [Fact]
    public void CreateStatic_ReturnsNonNullDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] items = ["item1", "item2"];

        var result = factory.CreateStatic(items);

        Assert.NotNull(result);
    }

    [Fact]
    public void CreateStatic_ReturnsStaticCollectionDataSourceType()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] items = ["item1", "item2"];

        var result = factory.CreateStatic(items);

        Assert.IsType<StaticCollectionDataSource<string>>(result);
    }

    [Fact]
    public void CreateStatic_ReturnsICollectionDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] items = ["item1", "item2"];

        var result = factory.CreateStatic(items);

        Assert.IsAssignableFrom<ICollectionDataSource<string>>(result);
    }

    [Fact]
    public void CreateStatic_ContainsCorrectItems()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] expectedItems = ["alpha", "beta", "gamma"];

        var result = factory.CreateStatic(expectedItems);

        Assert.Equal(expectedItems, result.Collection);
    }

    [Fact]
    public void CreateStatic_WithEmptyCollection_ReturnsEmptyDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] items = [];

        var result = factory.CreateStatic(items);

        Assert.Empty(result.Collection);
    }

    [Fact]
    public void CreateStatic_MaterializesCollection()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var enumerationCount = 0;
        IEnumerable<int> LazyEnumerable()
        {
            enumerationCount++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        var result = factory.CreateStatic(LazyEnumerable());

        Assert.Equal(1, enumerationCount);
        _ = result.Collection.ToList();
        _ = result.Collection.ToList();
        Assert.Equal(1, enumerationCount);
    }

    [Fact]
    public void CreateStatic_GetChangeToken_ReturnsNullChangeToken()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] items = ["item1"];

        var result = factory.CreateStatic(items);

        var changeToken = result.GetChangeToken();
        Assert.Same(NullChangeToken.Singleton, changeToken);
    }

    [Fact]
    public void CreateStatic_WithIntType_Works()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        int[] expectedItems = [1, 2, 3, 4, 5];

        var result = factory.CreateStatic(expectedItems);

        Assert.Equal(expectedItems, result.Collection);
    }

    [Fact]
    public void CreateStatic_DefaultOwnsIsTrue()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var disposableItem = new Mock<IDisposable>(MockBehavior.Strict);
        disposableItem.Setup(x => x.Dispose());
        IDisposable[] items = [disposableItem.Object];

        var result = factory.CreateStatic(items);
        (result as IDisposable)?.Dispose();

        disposableItem.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void CreateStatic_WithOwnsFalse_DoesNotDisposeItems()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var disposableItem = new Mock<IDisposable>(MockBehavior.Strict);
        IDisposable[] items = [disposableItem.Object];

        var result = factory.CreateStatic(items, owns: false);
        (result as IDisposable)?.Dispose();

        disposableItem.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public void CreateStatic_WithOwnsTrue_DisposesItems()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var disposableItem = new Mock<IDisposable>(MockBehavior.Strict);
        disposableItem.Setup(x => x.Dispose());
        IDisposable[] items = [disposableItem.Object];

        var result = factory.CreateStatic(items, owns: true);
        (result as IDisposable)?.Dispose();

        disposableItem.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region CreateObservable Tests

    [Fact]
    public void CreateObservable_ReturnsNonNullDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["item1", "item2"]);

        using var result = factory.CreateObservable(observableCollection);

        Assert.NotNull(result);
    }

    [Fact]
    public void CreateObservable_ReturnsObservableCollectionDataSourceType()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["item1", "item2"]);

        using var result = factory.CreateObservable(observableCollection);

        Assert.IsType<ObservableCollectionDataSource<string>>(result);
    }

    [Fact]
    public void CreateObservable_ReturnsIDisposableCollectionDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["item1", "item2"]);

        using var result = factory.CreateObservable(observableCollection);

        Assert.IsAssignableFrom<IDisposableCollectionDataSource<string>>(result);
    }

    [Fact]
    public void CreateObservable_ContainsCorrectItems()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["alpha", "beta", "gamma"]);

        using var result = factory.CreateObservable(observableCollection);

        Assert.Equal(["alpha", "beta", "gamma"], result.Collection);
    }

    [Fact]
    public void CreateObservable_WithEmptyCollection_ReturnsEmptyDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>();

        using var result = factory.CreateObservable(observableCollection);

        Assert.Empty(result.Collection);
    }

    [Fact]
    public void CreateObservable_ReflectsChangesToUnderlyingCollection()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["item1"]);

        using var result = factory.CreateObservable(observableCollection);

        observableCollection.Add("item2");

        Assert.Equal(["item1", "item2"], result.Collection);
    }

    [Fact]
    public void CreateObservable_GetChangeToken_ReturnsValidChangeToken()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["item1"]);

        using var result = factory.CreateObservable(observableCollection);

        var changeToken = result.GetChangeToken();
        Assert.NotNull(changeToken);
        Assert.False(changeToken.HasChanged);
    }

    [Fact]
    public void CreateObservable_ChangeToken_SignalsOnCollectionChange()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<string>(["item1"]);

        using var result = factory.CreateObservable(observableCollection);
        var changeToken = result.GetChangeToken();

        observableCollection.Add("item2");

        Assert.True(changeToken.HasChanged);
    }

    [Fact]
    public void CreateObservable_WithIntType_Works()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);

        using var result = factory.CreateObservable(observableCollection);

        Assert.Equal([1, 2, 3], result.Collection);
    }

    #endregion

    #region CreatePeriodicPolling Tests

    [Fact]
    public async Task CreatePeriodicPolling_ReturnsNonNullDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["item1", "item2"];

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>())
        );

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreatePeriodicPolling_ReturnsPeriodicPollingCollectionDataSourceType()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["item1"];

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>())
        );

        Assert.IsType<PeriodicPollingCollectionDataSource<string, object?>>(result);
    }

    [Fact]
    public async Task CreatePeriodicPolling_ReturnsIAsyncDisposableCollectionDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["item1"];

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>())
        );

        Assert.IsAssignableFrom<IAsyncDisposableCollectionDataSource<string>>(result);
    }

    [Fact]
    public async Task CreatePeriodicPolling_ContainsInitialItems()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["alpha", "beta", "gamma"];

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>())
        );

        Assert.Equal(initialCollection, result.Collection);
    }

    [Fact]
    public async Task CreatePeriodicPolling_WithEmptyInitialCollection_ReturnsEmptyDataSource()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = [];

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>())
        );

        Assert.Empty(result.Collection);
    }

    [Fact]
    public async Task CreatePeriodicPolling_WithState_PassesStateToRefreshDelegate()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        const string expectedState = "test-state";
        string? receivedState = null;
        string[] initialCollection = ["item1"];

        await using var result = factory.CreatePeriodicPolling(
            state: expectedState,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromMilliseconds(50),
            refreshCollectionAsync: (state, _, _) =>
            {
                receivedState = state;
                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>());
            }
        );

        await Task.Delay(100);

        Assert.Equal(expectedState, receivedState);
    }

    [Fact]
    public async Task CreatePeriodicPolling_WithIntType_Works()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        int[] initialCollection = [1, 2, 3];

        await using var result = factory.CreatePeriodicPolling<int, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>())
        );

        Assert.Equal(initialCollection, result.Collection);
    }

    [Fact]
    public async Task CreatePeriodicPolling_GetChangeToken_ReturnsValidChangeToken()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["item1"];

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromHours(1),
            refreshCollectionAsync: (_, _, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>())
        );

        var changeToken = result.GetChangeToken();
        Assert.NotNull(changeToken);
        Assert.False(changeToken.HasChanged);
    }

    [Fact]
    public async Task CreatePeriodicPolling_RefreshUpdatesCollection()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["item1"];
        string[] updatedCollection = ["item1", "item2", "item3"];
        var refreshCount = 0;

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromMilliseconds(50),
            refreshCollectionAsync: (_, _, _) =>
            {
                refreshCount++;
                return refreshCount == 1
                    ? ValueTask.FromResult(RefreshCollectionResultFactory.Changed(updatedCollection))
                    : ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>());
            }
        );

        await Task.Delay(150);

        Assert.Equal(updatedCollection, result.Collection);
    }

    [Fact]
    public async Task CreatePeriodicPolling_WithCustomExceptionHandler_HandlesExceptions()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        string[] initialCollection = ["item1"];
        var exceptionHandled = false;
        var refreshCount = 0;

        await using var result = factory.CreatePeriodicPolling<string, object?>(
            state: null,
            initialCollection: initialCollection,
            refreshInterval: TimeSpan.FromMilliseconds(50),
            refreshCollectionAsync: (_, _, _) =>
            {
                refreshCount++;
                if (refreshCount == 1)
                {
                    throw new InvalidOperationException("Test exception");
                }
                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>());
            },
            handleExceptionAsync: (_, _) =>
            {
                exceptionHandled = true;
                return ValueTask.CompletedTask;
            }
        );

        await Task.Delay(150);

        Assert.True(exceptionHandled);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void CreateStatic_IsThreadSafe_MultipleCallsReturnIndependentDataSources()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        const int threadCount = 10;
        var dataSources = new ICollectionDataSource<string>[threadCount];

        Parallel.For(0, threadCount, i =>
        {
            dataSources[i] = factory.CreateStatic([$"item{i}"]);
        });

        for (var i = 0; i < threadCount; i++)
        {
            Assert.NotNull(dataSources[i]);
            Assert.Single(dataSources[i].Collection);
            Assert.Contains($"item{i}", dataSources[i].Collection);
        }
    }

    [Fact]
    public void CreateObservable_IsThreadSafe_MultipleCallsReturnIndependentDataSources()
    {
        var factory = new DefaultCollectionDataSourceFactory();
        const int threadCount = 10;
        var dataSources = new IDisposableCollectionDataSource<string>[threadCount];

        Parallel.For(0, threadCount, i =>
        {
            var observableCollection = new ObservableCollection<string>([$"item{i}"]);
            dataSources[i] = factory.CreateObservable(observableCollection);
        });

        for (var i = 0; i < threadCount; i++)
        {
            Assert.NotNull(dataSources[i]);
            Assert.Single(dataSources[i].Collection);
            Assert.Contains($"item{i}", dataSources[i].Collection);
            dataSources[i].Dispose();
        }
    }

    #endregion
}
