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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using NCode.Collections.Providers.PeriodicPolling;
using NCode.Disposables;

#if NET10_0_OR_GREATER
using LockAlias = System.Threading.Lock;
#else
using LockAlias = object;
#endif

namespace NCode.Collections.Providers.DataSources;

/// <summary>
/// Provides an implementation of <see cref="ICollectionDataSource{T}"/> that periodically polls
/// for changes from a remote or external data source.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <typeparam name="TState">
/// The type of the state parameter passed to the refresh delegate. This can be used to provide
/// context such as API clients, database connections, or configuration.
/// </typeparam>
/// <remarks>
/// <para>
/// This data source uses a <see cref="PeriodicTimer"/> to periodically invoke a refresh delegate
/// that fetches updated data from an external source. If the refresh indicates changes occurred,
/// the collection is updated and the change token is signaled.
/// </para>
/// <para>
/// The polling mechanism waits for the specified interval after each refresh completes before
/// starting the next refresh, preventing overlapping requests for long-running operations.
/// </para>
/// <para>
/// This is ideal for scenarios where data must be fetched from sources that don't provide
/// push notifications, such as REST APIs, databases, or file systems.
/// </para>
/// <para>
/// Exception handling is customizable via the <see cref="HandleExceptionAsyncDelegate"/>. By default,
/// exceptions are re-thrown, which stops the polling. Provide a custom handler to log errors
/// and continue polling.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="RefreshCollectionAsyncDelegate{TItem,TState}"/>
/// <seealso cref="HandleExceptionAsyncDelegate"/>
/// <seealso cref="ICollectionDataSourceFactory.CreatePeriodicPolling{TItem,TState}"/>
[PublicAPI]
public sealed class PeriodicPollingCollectionDataSource<TItem, TState>
    : IAsyncDisposableCollectionDataSource<TItem>
{
    private LockAlias SyncObj { get; } = new();
    private bool IsDisposed { get; set; }
    private CancellationTokenSource? ChangeTokenSource { get; set; }
    private CancellationChangeToken? ConsumerChangeToken { get; set; }

    private TState State { get; }
    private IReadOnlyCollection<TItem> CurrentCollection { get; set; }
    private RefreshCollectionAsyncDelegate<TItem, TState> RefreshCollectionAsync { get; }
    private HandleExceptionAsyncDelegate HandleExceptionAsync { get; }

    private CancellationTokenSource PeriodicCancellationSource { get; } = new();
    private PeriodicTimer PeriodicTimer { get; }
    private Task PeriodicTimerTask { get; }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Returns the current cached collection. This collection is updated asynchronously by the
    /// periodic polling mechanism whenever the refresh delegate indicates changes have occurred.
    /// </para>
    /// <para>
    /// The collection is available immediately upon construction using the initial collection
    /// provided to the constructor.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the data source has been disposed.</exception>
    public IEnumerable<TItem> Collection
    {
        get
        {
            ThrowIfDisposed();
            return CurrentCollection;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PeriodicPollingCollectionDataSource{TItem,TState}"/> class
    /// and immediately starts the periodic polling timer.
    /// </summary>
    /// <param name="state">
    /// The state instance passed to the <paramref name="refreshCollectionAsync"/> delegate on each
    /// polling interval. This enables stateful refresh operations without closures.
    /// </param>
    /// <param name="initialCollection">
    /// The initial collection of items to use before the first refresh completes. This allows
    /// the data source to provide data immediately upon construction.
    /// </param>
    /// <param name="refreshInterval">
    /// The time interval between polling attempts. The timer starts after each refresh completes
    /// (successful or not), not from the start of each refresh.
    /// </param>
    /// <param name="refreshCollectionAsync">
    /// The delegate invoked periodically to fetch updated data from the external source.
    /// The delegate receives the current collection and state, and returns a result indicating
    /// whether the data has changed.
    /// </param>
    /// <param name="handleExceptionAsync">
    /// An optional delegate invoked when <paramref name="refreshCollectionAsync"/> throws an exception.
    /// If not provided (or set to <c>null</c>), the default behavior is to re-throw the exception,
    /// which stops the periodic polling. Provide a custom handler to log errors and continue polling.
    /// </param>
    /// <remarks>
    /// <para>
    /// The polling timer starts immediately upon construction. The first refresh occurs after
    /// the initial <paramref name="refreshInterval"/> elapses.
    /// </para>
    /// </remarks>
    public PeriodicPollingCollectionDataSource(
        TState state,
        IReadOnlyCollection<TItem> initialCollection,
        TimeSpan refreshInterval,
        RefreshCollectionAsyncDelegate<TItem, TState> refreshCollectionAsync,
        HandleExceptionAsyncDelegate? handleExceptionAsync = null
    )
    {
        State = state;
        CurrentCollection = initialCollection;
        RefreshCollectionAsync = refreshCollectionAsync;
        HandleExceptionAsync = handleExceptionAsync ?? DefaultHandleExceptionAsync;

        PeriodicTimer = new PeriodicTimer(refreshInterval);
        PeriodicTimerTask = Task.Run(async () => await PeriodicTimerAsync());
    }

    private static ValueTask DefaultHandleExceptionAsync(
        ExceptionDispatchInfo exception,
        CancellationToken cancellationToken
    )
    {
        exception.Throw();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;

        List<IDisposable?>? disposables;

        lock (SyncObj)
        {
            if (IsDisposed) return;
            IsDisposed = true;

            disposables =
            [
                PeriodicTimer,
                PeriodicTimerTask,
                PeriodicCancellationSource,
                ChangeTokenSource
            ];

            CurrentCollection = [];

            ChangeTokenSource = null;
            ConsumerChangeToken = null;
        }

        await PeriodicCancellationSource.CancelAsync();
        await PeriodicTimerTask;

        disposables.DisposeAll();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The change token is lazily initialized on first access. The returned token signals when
    /// the periodic refresh detects changes and updates the collection.
    /// </para>
    /// <para>
    /// Consumers should call this method again after receiving a change notification to
    /// obtain a new token for subsequent changes.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the data source has been disposed.</exception>
    public IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return ConsumerChangeToken;
    }

    [MemberNotNull(nameof(ConsumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        if (ConsumerChangeToken is not null) return;
        lock (SyncObj)
        {
            if (ConsumerChangeToken is not null) return;

            ThrowIfDisposed();
            RefreshChangeToken();
        }
    }

    [MemberNotNull(nameof(ConsumerChangeToken))]
    private void RefreshChangeToken()
    {
        ChangeTokenSource = new CancellationTokenSource();
        ConsumerChangeToken = new CancellationChangeToken(ChangeTokenSource.Token);
    }

    private async ValueTask PeriodicTimerAsync()
    {
        var cancellationToken = PeriodicCancellationSource.Token;
        try
        {
            while (await PeriodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await InvokeRefreshCollectionAsync(cancellationToken);
                }
                catch (OperationCanceledException exception)
                    when (exception.CancellationToken == cancellationToken)
                {
                    return;
                }
                catch (Exception exception)
                {
                    await HandleExceptionAsync(
                        ExceptionDispatchInfo.Capture(exception),
                        cancellationToken
                    );
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private async ValueTask InvokeRefreshCollectionAsync(CancellationToken cancellationToken)
    {
        var result = await RefreshCollectionAsync(State, CurrentCollection, cancellationToken);
        if (result.IsChanged)
        {
            await NotifyChangeAsync(result.NewCollection);
        }
    }

    private async ValueTask NotifyChangeAsync(IReadOnlyCollection<TItem> newCollection)
    {
        if (IsDisposed) return;

        CancellationTokenSource? oldTokenSource = null;
        try
        {
            lock (SyncObj)
            {
                if (IsDisposed) return;

                oldTokenSource = ChangeTokenSource;

                ChangeTokenSource = null;
                ConsumerChangeToken = null;

                RefreshChangeToken();

                CurrentCollection = newCollection;
            }

            if (oldTokenSource is not null)
            {
                await oldTokenSource.CancelAsync();
            }
        }
        finally
        {
            oldTokenSource?.Dispose();
        }
    }
}
