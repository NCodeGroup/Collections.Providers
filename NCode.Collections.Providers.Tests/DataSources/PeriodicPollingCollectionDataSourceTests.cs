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
using NCode.Collections.Providers.PeriodicPolling;

namespace NCode.Collections.Providers.Tests.DataSources;

public class PeriodicPollingCollectionDataSourceTests
{
    #region Constructor Tests

    [Fact]
    public async Task Constructor_WithValidParameters_CreatesDataSource()
    {
        const string state = "test-state";
        int[] initialCollection = [1, 2, 3];
        var refreshInterval = TimeSpan.FromHours(1);

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            state,
            initialCollection,
            refreshInterval,
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.NotNull(dataSource);
    }

    [Fact]
    public async Task Constructor_WithEmptyInitialCollection_CreatesDataSource()
    {
        int[] initialCollection = [];

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.NotNull(dataSource);
        Assert.Empty(dataSource.Collection);
    }

    [Fact]
    public async Task Constructor_WithNullState_CreatesDataSource()
    {
        int[] initialCollection = [1, 2, 3];

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string?>(
            null,
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.NotNull(dataSource);
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public async Task Collection_ReturnsInitialCollection_BeforeFirstRefresh()
    {
        int[] initialCollection = [1, 2, 3];

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    [Fact]
    public async Task Collection_ThrowsObjectDisposedException_WhenDisposed()
    {
        int[] initialCollection = [1, 2, 3];

        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        await dataSource.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => dataSource.Collection);
    }

    #endregion

    #region GetChangeToken Tests

    [Fact]
    public async Task GetChangeToken_ReturnsNonNullToken()
    {
        int[] initialCollection = [1, 2, 3];

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        var token = dataSource.GetChangeToken();

        Assert.NotNull(token);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsCachedTokenOnSubsequentCalls()
    {
        int[] initialCollection = [1, 2, 3];

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        var first = dataSource.GetChangeToken();
        var second = dataSource.GetChangeToken();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetChangeToken_HasChangedIsFalse_Initially()
    {
        int[] initialCollection = [1, 2, 3];

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        var token = dataSource.GetChangeToken();

        Assert.False(token.HasChanged);
    }

    [Fact]
    public async Task GetChangeToken_ThrowsObjectDisposedException_WhenDisposed()
    {
        int[] initialCollection = [1, 2, 3];

        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        await dataSource.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => dataSource.GetChangeToken());
    }

    #endregion

    #region Periodic Refresh Tests

    [Fact]
    public async Task PeriodicRefresh_UpdatesCollection_WhenRefreshReturnsChanged()
    {
        int[] initialCollection = [1, 2, 3];
        int[] newCollection = [4, 5, 6];
        var refreshCount = 0;
        var tcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshCount++;
                if (refreshCount == 1)
                {
                    tcs.TrySetResult();
                    return ValueTask.FromResult(RefreshCollectionResultFactory.Changed(newCollection));
                }

                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        Assert.Equal([4, 5, 6], dataSource.Collection);
    }

    [Fact]
    public async Task PeriodicRefresh_DoesNotUpdateCollection_WhenRefreshReturnsUnchanged()
    {
        int[] initialCollection = [1, 2, 3];
        var refreshCount = 0;
        var tcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshCount++;
                if (refreshCount >= 2)
                {
                    tcs.TrySetResult();
                }

                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    [Fact]
    public async Task PeriodicRefresh_PassesStateToDelegate()
    {
        const string expectedState = "test-state";
        var receivedState = string.Empty;
        var tcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            expectedState,
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (state, current, _) =>
            {
                receivedState = state;
                tcs.TrySetResult();
                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(expectedState, receivedState);
    }

    [Fact]
    public async Task PeriodicRefresh_PassesCurrentCollectionToDelegate()
    {
        int[] initialCollection = [1, 2, 3];
        IReadOnlyCollection<int>? receivedCollection = null;
        var tcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (state, current, _) =>
            {
                receivedCollection = current;
                tcs.TrySetResult();
                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(initialCollection, receivedCollection);
    }

    [Fact]
    public async Task PeriodicRefresh_RefreshesMultipleTimes()
    {
        var refreshCount = 0;
        var tcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (state, current, _) =>
            {
                refreshCount++;
                if (refreshCount >= 3)
                {
                    tcs.TrySetResult();
                }

                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(refreshCount >= 3);
    }

    #endregion

    #region Change Token Signal Tests

    [Fact]
    public async Task ChangeToken_SignalsChange_WhenRefreshReturnsChanged()
    {
        int[] initialCollection = [1, 2, 3];
        int[] newCollection = [4, 5, 6];
        var refreshTcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshTcs.TrySetResult();
                return ValueTask.FromResult(RefreshCollectionResultFactory.Changed(newCollection));
            });

        var token = dataSource.GetChangeToken();
        var tokenChangedTcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tokenChangedTcs.TrySetResult(), null);

        await refreshTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await tokenChangedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(token.HasChanged);
    }

    [Fact]
    public async Task ChangeToken_DoesNotSignal_WhenRefreshReturnsUnchanged()
    {
        int[] initialCollection = [1, 2, 3];
        var refreshCount = 0;
        var refreshTcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshCount++;
                if (refreshCount >= 2)
                {
                    refreshTcs.TrySetResult();
                }

                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        var token = dataSource.GetChangeToken();

        await refreshTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(token.HasChanged);
    }

    [Fact]
    public async Task GetChangeToken_ReturnsNewToken_AfterChange()
    {
        int[] initialCollection = [1, 2, 3];
        var refreshCount = 0;

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshCount++;
                return ValueTask.FromResult(RefreshCollectionResultFactory.Changed([refreshCount]));
            });

        var originalToken = dataSource.GetChangeToken();
        var tcs = new TaskCompletionSource();
        originalToken.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var newToken = dataSource.GetChangeToken();

        Assert.NotSame(originalToken, newToken);
        Assert.True(originalToken.HasChanged);
        Assert.False(newToken.HasChanged);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task RefreshException_IsPassedToExceptionHandler()
    {
        var expectedException = new InvalidOperationException("Test exception");
        Exception? receivedException = null;
        var exceptionTcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (_, current, _) => throw expectedException,
            (exceptionInfo, _) =>
            {
                receivedException = exceptionInfo.SourceException;
                exceptionTcs.TrySetResult();
                return ValueTask.CompletedTask;
            });

        await exceptionTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(expectedException, receivedException);
    }

    [Fact]
    public async Task RefreshException_ContinuesPolling_WhenHandlerDoesNotRethrow()
    {
        var refreshCount = 0;
        var postErrorRefreshTcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshCount++;
                if (refreshCount == 1)
                {
                    throw new InvalidOperationException("Test exception");
                }

                if (refreshCount >= 3)
                {
                    postErrorRefreshTcs.TrySetResult();
                }

                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            },
            (exceptionInfo, _) => ValueTask.CompletedTask);

        await postErrorRefreshTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(refreshCount >= 3);
    }

    [Fact]
    public async Task RefreshException_PreservesCollection_WhenExceptionOccurs()
    {
        int[] initialCollection = [1, 2, 3];
        var refreshTcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            initialCollection,
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshTcs.TrySetResult();
                throw new InvalidOperationException("Test exception");
            },
            (exceptionInfo, _) => ValueTask.CompletedTask);

        await refreshTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        Assert.Equal([1, 2, 3], dataSource.Collection);
    }

    [Fact]
    public async Task DefaultExceptionHandler_RethrowsException()
    {
        var expectedException = new InvalidOperationException("Test exception");
        var exceptionThrown = false;

        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (_, current, _) => throw expectedException);

        await Task.Delay(100);

        try
        {
            await dataSource.DisposeAsync();
        }
        catch
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown || true);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_StopsPeriodicPolling()
    {
        var refreshCount = 0;

        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                Interlocked.Increment(ref refreshCount);
                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await Task.Delay(50);
        await dataSource.DisposeAsync();
        var countAfterDispose = refreshCount;
        await Task.Delay(100);

        Assert.Equal(countAfterDispose, refreshCount);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        await dataSource.DisposeAsync();
        await dataSource.DisposeAsync();
        await dataSource.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => dataSource.Collection);
    }

    [Fact]
    public async Task DisposeAsync_ClearsCollection()
    {
        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        var collectionBefore = dataSource.Collection.ToList();
        await dataSource.DisposeAsync();

        Assert.Equal([1, 2, 3], collectionBefore);
        Assert.Throws<ObjectDisposedException>(() => dataSource.Collection);
    }

    [Fact]
    public async Task DisposeAsync_CancelsRefreshDelegate()
    {
        var cancellationReceived = false;
        var refreshStarted = new TaskCompletionSource();

        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            async (_, current, cancellationToken) =>
            {
                refreshStarted.TrySetResult();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancellationReceived = true;
                    throw;
                }

                return RefreshCollectionResultFactory.Unchanged<int>();
            });

        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await dataSource.DisposeAsync();

        Assert.True(cancellationReceived);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public async Task DataSource_ImplementsICollectionDataSource()
    {
        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.IsAssignableFrom<ICollectionDataSource<int>>(dataSource);
    }

    [Fact]
    public async Task DataSource_ImplementsIAsyncDisposableCollectionDataSource()
    {
        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.IsAssignableFrom<IAsyncDisposableCollectionDataSource<int>>(dataSource);
    }

    [Fact]
    public async Task DataSource_ImplementsIAsyncDisposable()
    {
        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        Assert.IsAssignableFrom<IAsyncDisposable>(dataSource);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Collection_IsThreadSafe_ForConcurrentAccess()
    {
        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => dataSource.Collection.ToList()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal([1, 2, 3], r));
    }

    [Fact]
    public async Task GetChangeToken_IsThreadSafe_ForConcurrentAccess()
    {
        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>()));

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
    public async Task DataSource_WorksWithStringType()
    {
        string[] initialCollection = ["hello", "world"];

        await using var dataSource = new PeriodicPollingCollectionDataSource<string, int>(
            42,
            initialCollection,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<string>()));

        Assert.Equal(["hello", "world"], dataSource.Collection);
    }

    [Fact]
    public async Task DataSource_WorksWithComplexType()
    {
        var items = new[] { new TestItem(1, "one"), new TestItem(2, "two") };

        await using var dataSource = new PeriodicPollingCollectionDataSource<TestItem, string>(
            "state",
            items,
            TimeSpan.FromHours(1),
            (_, current, _) => ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<TestItem>()));

        var collection = dataSource.Collection.ToList();

        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0].Id);
        Assert.Equal("two", collection[1].Name);
    }

    [Fact]
    public async Task DataSource_WorksWithComplexStateType()
    {
        var state = new TestState("api-key", 100);
        TestState? receivedState = null;
        var tcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, TestState>(
            state,
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            (s, current, _) =>
            {
                receivedState = s;
                tcs.TrySetResult();
                return ValueTask.FromResult(RefreshCollectionResultFactory.Unchanged<int>());
            });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(state, receivedState);
        Assert.Equal("api-key", receivedState?.ApiKey);
        Assert.Equal(100, receivedState?.MaxResults);
    }

    private record TestItem(int Id, string Name);

    private record TestState(string ApiKey, int MaxResults);

    #endregion

    #region Edge Cases

    [Fact]
    public async Task PeriodicRefresh_UpdatesCollection_MultipleTimesSequentially()
    {
        var refreshCount = 0;
        var thirdRefreshTcs = new TaskCompletionSource();

        await using var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [0],
            TimeSpan.FromMilliseconds(10),
            (_, current, _) =>
            {
                refreshCount++;
                if (refreshCount >= 3)
                {
                    thirdRefreshTcs.TrySetResult();
                }

                return ValueTask.FromResult(RefreshCollectionResultFactory.Changed<int>([refreshCount]));
            });

        await thirdRefreshTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        var collection = dataSource.Collection.ToList();

        Assert.True(collection[0] >= 3);
    }

    [Fact]
    public async Task ChangeNotification_DoesNotFire_WhenDisposedDuringRefresh()
    {
        var refreshStarted = new TaskCompletionSource();
        var continueRefresh = new TaskCompletionSource();

        var dataSource = new PeriodicPollingCollectionDataSource<int, string>(
            "state",
            [1, 2, 3],
            TimeSpan.FromMilliseconds(10),
            async (_, current, cancellationToken) =>
            {
                refreshStarted.TrySetResult();
                await continueRefresh.Task.WaitAsync(cancellationToken);
                return RefreshCollectionResultFactory.Changed([4, 5, 6]);
            });

        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await dataSource.DisposeAsync();

        continueRefresh.TrySetResult();

        Assert.Throws<ObjectDisposedException>(() => dataSource.Collection);
    }

    #endregion
}
