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
using NCode.Disposables;

#if NET10_0_OR_GREATER
using LockAlias = System.Threading.Lock;
#else
using LockAlias = object;
#endif

namespace NCode.Collections.Providers.DataSources;

/// <summary>
/// Provides an implementation of <see cref="ICollectionDataSource{T}"/> that aggregates multiple
/// data sources into a single unified collection.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This class combines items from multiple <see cref="ICollectionDataSource{T}"/> instances into
/// a single collection. By default, items are concatenated in the order the data sources were provided,
/// but this behavior can be customized via the <see cref="CombineFunc"/> property.
/// </para>
/// <para>
/// The composite data source subscribes to change tokens from all underlying data sources and
/// signals its own change token when any source changes, allowing consumers to receive consolidated
/// change notifications.
/// </para>
/// <para>
/// By default, this class does not take ownership of the individual data sources (<see cref="Owns"/>
/// defaults to <c>false</c>) because data sources are typically resolved from dependency injection,
/// which manages their lifetimes. Set <see cref="Owns"/> to <c>true</c> if the composite should
/// dispose the data sources when it is disposed.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="ICollectionDataSourceFactory.CreateComposite{T}"/>
[PublicAPI]
public sealed class CompositeCollectionDataSource<T> : IAsyncDisposableCollectionDataSource<T>
{
    private LockAlias SyncObj { get; } = new();
    private bool IsDisposed { get; set; }
    private CancellationTokenSource? ChangeTokenSource { get; set; }
    private IChangeToken? ConsumerChangeToken { get; set; }
    private List<IDisposable>? ChangeTokenRegistrations { get; set; }

    /// <summary>
    /// Gets or initializes a value indicating whether this composite owns the individual data sources.
    /// </summary>
    /// <value>
    /// <c>true</c> if the composite should dispose the individual data sources when it is disposed;
    /// <c>false</c> if the caller or DI container retains ownership. Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When data sources are resolved from dependency injection, ownership should typically remain
    /// with the DI container (<c>false</c>). Set to <c>true</c> when the composite is responsible
    /// for the full lifecycle of manually created data sources.
    /// </para>
    /// </remarks>
    public bool Owns { get; init; }

    /// <summary>
    /// Gets or initializes a function that combines items from multiple data sources into a single enumerable.
    /// </summary>
    /// <value>
    /// A function that takes the collections from all data sources and returns a combined enumerable.
    /// Defaults to concatenating all items using <see cref="Enumerable.SelectMany{TSource,TResult}(IEnumerable{TSource},Func{TSource,IEnumerable{TResult}})"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Override this property to customize how items from multiple data sources are combined.
    /// For example, you could implement deduplication, sorting, or filtering logic.
    /// </para>
    /// <example>
    /// <code>
    /// // Custom combine function that removes duplicates
    /// CombineFunc = collections => collections.SelectMany(c => c).Distinct()
    /// </code>
    /// </example>
    /// </remarks>
    public Func<IEnumerable<IEnumerable<T>>, IEnumerable<T>> CombineFunc { get; init; } = DefaultCombineFunc;

    private IReadOnlyList<ICollectionDataSource<T>> DataSources { get; }
    private IReadOnlyCollection<ISupportChangeToken> ChangeTokenProducers { get; }
    private IEnumerable<T>? CollectionOrNull { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The collection is lazily initialized on first access and automatically refreshed
    /// when any underlying data source signals a change.
    /// </para>
    /// <para>
    /// Items are combined using the <see cref="CombineFunc"/> function.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the composite has been disposed.</exception>
    public IEnumerable<T> Collection
    {
        get
        {
            EnsureCollectionInitialized();
            return CollectionOrNull;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeCollectionDataSource{T}"/> class
    /// with the specified data sources.
    /// </summary>
    /// <param name="dataSources">
    /// A collection of <see cref="ICollectionDataSource{T}"/> instances whose items will be
    /// aggregated. The data sources are enumerated immediately and stored.
    /// </param>
    /// <remarks>
    /// The composite subscribes to change tokens from all data sources and consolidates
    /// their change notifications into a single change token.
    /// </remarks>
    public CompositeCollectionDataSource(IEnumerable<ICollectionDataSource<T>> dataSources)
    {
        var dataSourcesList = dataSources.ToList();

        DataSources = dataSourcesList;
        ChangeTokenProducers = dataSourcesList;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeCollectionDataSource{T}"/> class
    /// with the specified data sources and additional change token producers.
    /// </summary>
    /// <param name="dataSources">
    /// A collection of <see cref="ICollectionDataSource{T}"/> instances whose items will be
    /// aggregated. The data sources are enumerated immediately and stored.
    /// </param>
    /// <param name="additionalChangeTokenProducers">
    /// Additional <see cref="ISupportChangeToken"/> instances that trigger collection refresh
    /// when they signal changes. This allows the collection to be refreshed based on external
    /// signals beyond the data sources themselves (e.g., configuration changes).
    /// </param>
    /// <remarks>
    /// <para>
    /// The composite subscribes to change tokens from all data sources and additional producers,
    /// consolidating their change notifications into a single change token.
    /// </para>
    /// <para>
    /// Note that the additional change token producers are not owned by the composite and will
    /// not be disposed when the composite is disposed, regardless of the <see cref="Owns"/> setting.
    /// </para>
    /// </remarks>
    public CompositeCollectionDataSource(
        IEnumerable<ICollectionDataSource<T>> dataSources,
        IEnumerable<ISupportChangeToken> additionalChangeTokenProducers
    )
    {
        var dataSourcesList = dataSources.ToList();

        DataSources = dataSourcesList;
        ChangeTokenProducers = [..dataSourcesList, ..additionalChangeTokenProducers];
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The change token is lazily initialized on first access. The returned token signals when
    /// any underlying data source or additional change token producer signals a change.
    /// </para>
    /// <para>
    /// Consumers should call this method again after receiving a change notification to
    /// obtain a new token for subsequent changes.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the composite has been disposed.</exception>
    public IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return ConsumerChangeToken;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
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
                disposables.AddRange(DataSources);
            }

            if (ChangeTokenRegistrations is { Count: > 0 })
            {
                disposables.AddRange(ChangeTokenRegistrations);
            }

            if (ChangeTokenSource is not null)
            {
                disposables.Add(ChangeTokenSource);
            }

            CollectionOrNull = null;
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
            ChangeToken.OnChange(changeTokenProducer.GetChangeToken, HandleChange))
        );
    }

    private void HandleChange()
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
        }
        finally
        {
            oldTokenSource?.Dispose();
        }
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
        CollectionOrNull = CombineFunc(DataSources.Select(dataSource => dataSource.Collection));
    }

    private static IEnumerable<T> DefaultCombineFunc(IEnumerable<IEnumerable<T>> collections) =>
        collections.SelectMany(collection => collection);
}
