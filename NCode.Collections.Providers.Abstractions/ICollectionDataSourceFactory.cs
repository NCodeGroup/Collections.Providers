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
using JetBrains.Annotations;
using NCode.Collections.Providers.PeriodicPolling;

namespace NCode.Collections.Providers;

/// <summary>
/// Provides the ability to create instances of <see cref="ICollectionDataSource{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides methods to create various types of data sources for different
/// scenarios, including static collections, observable collections, composite aggregations,
/// and periodic polling from remote sources.
/// </para>
/// <para>
/// The factory should be registered as a singleton in dependency injection and used
/// to create data sources on-demand based on application requirements.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="ICollectionProviderFactory"/>
[PublicAPI]
public interface ICollectionDataSourceFactory
{
    /// <summary>
    /// Creates a single data source instance that combines multiple data sources into one.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="dataSources">
    /// The collection of data source instances to combine. Items from all data sources
    /// are aggregated into a single collection in enumeration order.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the created data source takes ownership of the provided data sources
    /// and will dispose them when disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    /// A new <see cref="IAsyncDisposableCollectionDataSource{T}"/> that aggregates items
    /// from all provided data sources and consolidates their change notifications.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The composite data source subscribes to change tokens from all underlying data sources
    /// and signals its own change token when any source changes. This is useful for combining
    /// multiple independent data sources into a single unified source.
    /// </para>
    /// </remarks>
    IAsyncDisposableCollectionDataSource<T> CreateComposite<T>(
        IEnumerable<ICollectionDataSource<T>> dataSources,
        bool owns = false
    );

    /// <summary>
    /// Creates a data source instance that wraps a static collection without change notifications.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">
    /// The collection of items to wrap. This collection should not be modified after
    /// being passed to this method.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the created data source takes ownership of the collection and will
    /// dispose it (if disposable) when disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <returns>
    /// A new <see cref="ICollectionDataSource{T}"/> that exposes the provided collection.
    /// The returned data source's change token will never signal changes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method for collections that are known at startup and will not change
    /// during the application lifetime. The returned data source provides an
    /// <see cref="INullChangeToken"/> that never signals.
    /// </para>
    /// </remarks>
    ICollectionDataSource<T> CreateStatic<T>(
        IEnumerable<T> collection,
        bool owns = true
    );

    /// <summary>
    /// Creates a data source instance that wraps an observable collection with automatic change notifications.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="observableCollection">
    /// The observable collection to wrap. The data source will subscribe to
    /// <see cref="ObservableCollection{T}.CollectionChanged"/> events to detect changes.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the created data source takes ownership of the observable collection
    /// and will dispose it (if disposable) when disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <returns>
    /// A new <see cref="IDisposableCollectionDataSource{T}"/> that exposes the observable
    /// collection and signals changes when the collection is modified.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is ideal for scenarios where the collection is modified through the
    /// <see cref="ObservableCollection{T}"/> API (Add, Remove, Clear, etc.) and consumers
    /// need to be notified of those changes automatically.
    /// </para>
    /// </remarks>
    IDisposableCollectionDataSource<T> CreateObservable<T>(
        ObservableCollection<T> observableCollection,
        bool owns = true
    );

    /// <summary>
    /// Creates a data source instance that periodically polls for updates from a remote source.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the collection.</typeparam>
    /// <typeparam name="TState">
    /// The type of the state parameter passed to the refresh delegate. This can be used
    /// to provide context such as API clients, connection strings, or configuration.
    /// </typeparam>
    /// <param name="state">
    /// The state instance passed to the <paramref name="refreshCollectionAsync"/> delegate
    /// on each polling interval. This enables stateful refresh operations without closures.
    /// </param>
    /// <param name="initialCollection">
    /// The initial collection of items to use before the first refresh completes.
    /// This allows the data source to provide data immediately upon creation.
    /// </param>
    /// <param name="refreshInterval">
    /// The time interval between polling attempts. The timer starts after each refresh
    /// completes (successful or not), not from the start of each refresh.
    /// </param>
    /// <param name="refreshCollectionAsync">
    /// The delegate invoked periodically to fetch updated data from the remote source.
    /// The delegate receives the current collection and state, and returns the new collection
    /// or indicates no changes occurred.
    /// </param>
    /// <param name="handleExceptionAsync">
    /// An optional delegate invoked when <paramref name="refreshCollectionAsync"/> throws
    /// an exception. If not provided, exceptions are silently ignored and the existing
    /// collection is retained. Use this to implement logging, alerting, or retry logic.
    /// </param>
    /// <returns>
    /// A new <see cref="IAsyncDisposableCollectionDataSource{TItem}"/> that periodically
    /// refreshes its collection and signals changes when updates are detected.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This data source is ideal for scenarios where data is fetched from external sources
    /// such as databases, APIs, or configuration services that don't provide push notifications.
    /// </para>
    /// <para>
    /// The polling mechanism uses a timer that waits for the refresh interval after each
    /// refresh completes, ensuring that long-running refresh operations don't cause overlapping
    /// requests.
    /// </para>
    /// </remarks>
    /// <seealso cref="RefreshCollectionAsyncDelegate{TItem,TState}"/>
    /// <seealso cref="HandleExceptionAsyncDelegate"/>
    /// <seealso cref="RefreshCollectionResult{TItem}"/>
    IAsyncDisposableCollectionDataSource<TItem> CreatePeriodicPolling<TItem, TState>(
        TState state,
        IReadOnlyCollection<TItem> initialCollection,
        TimeSpan refreshInterval,
        RefreshCollectionAsyncDelegate<TItem, TState> refreshCollectionAsync,
        HandleExceptionAsyncDelegate? handleExceptionAsync = default
    );
}
