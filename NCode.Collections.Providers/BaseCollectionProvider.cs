#region Copyright Preamble

//
//    Copyright @ 2023 NCode Group
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
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using NCode.Collections.Providers.DataSources;
using NCode.Disposables;

#if NET10_0_OR_GREATER
using LockAlias = System.Threading.Lock;
#else
using LockAlias = object;
#endif

namespace NCode.Collections.Providers;

/// <summary>
/// Provides a base implementation for the <see cref="ICollectionProvider{TItem,TCollection}"/> abstraction.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <typeparam name="TCollection">
/// The type of collection that contains the items. Must implement <see cref="IEnumerable{T}"/>
/// of <typeparamref name="TItem"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// This abstract class provides the core implementation for collection providers, including:
/// </para>
/// <list type="bullet">
///   <item><description>Thread-safe lazy initialization of the collection</description></item>
///   <item><description>Change token management and propagation from data sources</description></item>
///   <item><description>Automatic collection refresh when underlying data sources change</description></item>
///   <item><description>Proper disposal of owned resources</description></item>
/// </list>
/// <para>
/// Derived classes must implement <see cref="CreateCollection"/> to specify how the
/// <typeparamref name="TCollection"/> is constructed from the source items.
/// </para>
/// <para>
/// This class also implements <see cref="ICollectionDataSource{T}"/> to allow providers
/// to be composed hierarchically.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionProvider{TItem,TCollection}"/>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="CollectionProvider{TItem,TCollection}"/>
[PublicAPI]
public abstract class BaseCollectionProvider<TItem, TCollection> :
    ICollectionProvider<TItem, TCollection>,
    ICollectionDataSource<TItem>
    where TCollection : IEnumerable<TItem>
{
    private LockAlias SyncObj { get; } = new();
    private bool IsDisposed { get; set; }
    private CancellationTokenSource? ChangeTokenSource { get; set; }
    private IChangeToken? ConsumerChangeToken { get; set; }
    private List<IDisposable>? ChangeTokenRegistrations { get; set; }

    private bool Owns { get; }
    private ICollectionDataSource<TItem> DataSource { get; }
    private IReadOnlyCollection<ISupportChangeToken> ChangeTokenProducers { get; }
    private TCollection? CollectionOrNull { get; set; }

    IEnumerable<TItem> ICollectionDataSource<TItem>.Collection => Collection;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The collection is lazily initialized on first access and automatically refreshed
    /// when any underlying data source signals a change.
    /// </para>
    /// <para>
    /// This property is thread-safe and can be accessed from multiple threads concurrently.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    public TCollection Collection
    {
        get
        {
            EnsureCollectionInitialized();
            return CollectionOrNull;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCollectionProvider{TItem,TCollection}"/> class
    /// with a single data source.
    /// </summary>
    /// <param name="dataSource">The <see cref="ICollectionDataSource{T}"/> instance that provides items for the collection.</param>
    /// <param name="owns">
    /// When <c>true</c>, the provider takes ownership of the data source and will dispose it
    /// when the provider is disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <remarks>
    /// Use this constructor when you have a single data source and want the provider to
    /// manage its lifetime.
    /// </remarks>
    protected BaseCollectionProvider(
        ICollectionDataSource<TItem> dataSource,
        bool owns = true
    )
    {
        Owns = owns;
        DataSource = dataSource;
        ChangeTokenProducers = [dataSource];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCollectionProvider{TItem,TCollection}"/> class
    /// with a single data source and additional change token producers.
    /// </summary>
    /// <param name="dataSource">The <see cref="ICollectionDataSource{T}"/> instance that provides items for the collection.</param>
    /// <param name="additionalChangeTokenProducers">
    /// Additional <see cref="ISupportChangeToken"/> instances that trigger collection refresh when they signal changes.
    /// This allows the collection to be refreshed based on external signals beyond the data source itself.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the provider takes ownership of the data source and will dispose it
    /// when the provider is disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Use this constructor when the collection should be refreshed not only when the data source
    /// changes, but also when other external factors change (e.g., configuration changes).
    /// </para>
    /// <para>
    /// Note that the additional change token producers are not owned by the provider and will
    /// not be disposed when the provider is disposed.
    /// </para>
    /// </remarks>
    protected BaseCollectionProvider(
        ICollectionDataSource<TItem> dataSource,
        IEnumerable<ISupportChangeToken> additionalChangeTokenProducers,
        bool owns = true
    )
    {
        Owns = owns;
        DataSource = dataSource;
        ChangeTokenProducers = [dataSource, ..additionalChangeTokenProducers];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCollectionProvider{TItem,TCollection}"/> class
    /// with multiple data sources that are aggregated into a single collection.
    /// </summary>
    /// <param name="dataSources">
    /// A collection of <see cref="ICollectionDataSource{T}"/> instances whose items will be
    /// aggregated into a single collection.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the provider takes ownership of the individual data sources and will
    /// dispose them when the provider is disposed. When <c>false</c>, the caller (typically the
    /// DI container) retains ownership. Defaults to <c>false</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This constructor creates an internal composite data source to aggregate all provided
    /// data sources. The provider always owns this internal composite, but ownership of the
    /// individual data sources is controlled by the <paramref name="owns"/> parameter.
    /// </para>
    /// <para>
    /// The default value of <c>false</c> for <paramref name="owns"/> is appropriate when data
    /// sources are resolved from dependency injection, as the DI container manages their lifetimes.
    /// </para>
    /// </remarks>
    protected BaseCollectionProvider(
        IEnumerable<ICollectionDataSource<TItem>> dataSources,
        bool owns = false
    )
    {
        var dataSource = new CompositeCollectionDataSource<TItem>(dataSources)
        {
            Owns = owns
        };

        Owns = true; // this variant always own the composite data source
        DataSource = dataSource;
        ChangeTokenProducers = [dataSource];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCollectionProvider{TItem,TCollection}"/> class
    /// with multiple data sources and additional change token producers.
    /// </summary>
    /// <param name="dataSources">
    /// A collection of <see cref="ICollectionDataSource{T}"/> instances whose items will be
    /// aggregated into a single collection.
    /// </param>
    /// <param name="additionalChangeTokenProducers">
    /// Additional <see cref="ISupportChangeToken"/> instances that trigger collection refresh when they signal changes.
    /// This allows the collection to be refreshed based on external signals beyond the data sources.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the provider takes ownership of the individual data sources and will
    /// dispose them when the provider is disposed. When <c>false</c>, the caller (typically the
    /// DI container) retains ownership. Defaults to <c>false</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This constructor creates an internal composite data source to aggregate all provided
    /// data sources. The provider always owns this internal composite, but ownership of the
    /// individual data sources is controlled by the <paramref name="owns"/> parameter.
    /// </para>
    /// <para>
    /// Use this constructor when the collection should be refreshed not only when data sources
    /// change, but also when other external factors change (e.g., configuration changes).
    /// </para>
    /// <para>
    /// Note that the additional change token producers are not owned by the provider and will
    /// not be disposed when the provider is disposed.
    /// </para>
    /// </remarks>
    protected BaseCollectionProvider(
        IEnumerable<ICollectionDataSource<TItem>> dataSources,
        IEnumerable<ISupportChangeToken> additionalChangeTokenProducers,
        bool owns = false
    )
    {
        var dataSource = new CompositeCollectionDataSource<TItem>(dataSources)
        {
            Owns = owns
        };

        Owns = true; // this variant always own the composite data source
        DataSource = dataSource;
        ChangeTokenProducers = [dataSource, ..additionalChangeTokenProducers];
    }

    /// <summary>
    /// When overridden in a derived class, creates a new <typeparamref name="TCollection"/> instance
    /// from the provided items.
    /// </summary>
    /// <param name="items">
    /// The <see cref="IEnumerable{T}"/> containing all items from the underlying data source(s).
    /// </param>
    /// <returns>
    /// A new <typeparamref name="TCollection"/> instance containing the provided items.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called during initial collection creation and whenever the underlying
    /// data sources signal a change. Implementations should create a new collection instance
    /// rather than modifying an existing one.
    /// </para>
    /// <para>
    /// This method is called while holding a lock, so implementations should avoid long-running
    /// operations or operations that could cause deadlocks.
    /// </para>
    /// </remarks>
    protected abstract TCollection CreateCollection(IEnumerable<TItem> items);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The change token is lazily initialized on first access. The returned token will signal
    /// when any underlying data source changes, after the collection has been refreshed.
    /// </para>
    /// <para>
    /// Consumers should call this method again after receiving a change notification to
    /// obtain a new token for subsequent changes.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    public IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return ConsumerChangeToken;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting managed resources asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
    /// <remarks>
    /// <para>
    /// Override this method in derived classes to add custom disposal logic. Always call
    /// <c>await base.DisposeAsyncCore()</c> to ensure proper cleanup of base class resources.
    /// </para>
    /// <para>
    /// This method disposes of owned data sources, change token registrations, and the
    /// internal cancellation token source. The method is idempotent and can be called
    /// multiple times safely.
    /// </para>
    /// </remarks>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (IsDisposed) return;

        List<object>? disposables;

        lock (SyncObj)
        {
            if (IsDisposed) return;
            IsDisposed = true;

            disposables = [];

            if (Owns)
            {
                disposables.Add(DataSource);
            }

            if (ChangeTokenRegistrations is { Count: > 0 })
            {
                disposables.AddRange(ChangeTokenRegistrations);
            }

            if (ChangeTokenSource is not null)
            {
                disposables.Add(ChangeTokenSource);
            }

            CollectionOrNull = default;
            ChangeTokenRegistrations = null;

            ChangeTokenSource = null;
            ConsumerChangeToken = null;
        }

        await disposables.DisposeAllAsync();
    }

    [MemberNotNull(nameof(CollectionOrNull))]
    private void EnsureCollectionInitialized()
    {
        if (CollectionOrNull is not null) return;
        lock (SyncObj)
        {
            if (CollectionOrNull is not null) return;

            ThrowIfDisposed();
            EnsureChangeTokenInitialized();
            RefreshCollection();
        }
    }

    [MemberNotNull(nameof(ConsumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        if (ConsumerChangeToken is not null) return;
        lock (SyncObj)
        {
            if (ConsumerChangeToken is not null) return;

            ThrowIfDisposed();
            SubscribeChangeTokenProducers();
            RefreshConsumerChangeToken();
        }
    }

    private void SubscribeChangeTokenProducers()
    {
        ChangeTokenRegistrations ??= [];
        ChangeTokenRegistrations.AddRange(ChangeTokenProducers.Select(changeTokenProducer =>
            ChangeToken.OnChange(changeTokenProducer.GetChangeToken, HandleChange)));
    }

    private void HandleChange()
    {
        CancellationTokenSource? oldTokenSource;

        if (IsDisposed) return;
        lock (SyncObj)
        {
            if (IsDisposed) return;

            oldTokenSource = ChangeTokenSource;

            ChangeTokenSource = null;
            ConsumerChangeToken = null;

            // refresh the cached change token
            if (oldTokenSource is not null)
            {
                RefreshConsumerChangeToken();
            }

            // refresh the cached collection
            if (CollectionOrNull is not null)
            {
                RefreshCollection();
            }
        }

        // this will trigger the consumer change token
        // and most importantly after the collection has been refreshed
        oldTokenSource?.Cancel();

        oldTokenSource?.Dispose();
    }

    [MemberNotNull(nameof(ConsumerChangeToken))]
    private void RefreshConsumerChangeToken()
    {
        ChangeTokenSource = new CancellationTokenSource();
        ConsumerChangeToken = new CancellationChangeToken(ChangeTokenSource.Token);
    }

    [MemberNotNull(nameof(CollectionOrNull))]
    private void RefreshCollection()
    {
        CollectionOrNull = CreateCollection(DataSource.Collection);
    }
}
