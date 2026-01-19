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
using NCode.Collections.Providers.DataSources;
using NCode.Collections.Providers.PeriodicPolling;

namespace NCode.Collections.Providers;

/// <summary>
/// Provides the default implementation of the <see cref="ICollectionDataSourceFactory"/> abstraction.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates instances of the built-in data source implementations:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="CompositeCollectionDataSource{T}"/> for aggregating multiple data sources</description></item>
///   <item><description><see cref="StaticCollectionDataSource{T}"/> for immutable collections</description></item>
///   <item><description><see cref="ObservableCollectionDataSource{T}"/> for observable collections with automatic change notifications</description></item>
///   <item><description><see cref="PeriodicPollingCollectionDataSource{TItem,TState}"/> for periodic polling from external sources</description></item>
/// </list>
/// <para>
/// This class should be registered as a singleton in dependency injection. It is stateless
/// and thread-safe.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSourceFactory"/>
[PublicAPI]
public class DefaultCollectionDataSourceFactory : ICollectionDataSourceFactory
{
    /// <inheritdoc />
    /// <remarks>
    /// Creates a <see cref="CompositeCollectionDataSource{T}"/> that aggregates the provided data sources.
    /// </remarks>
    public IAsyncDisposableCollectionDataSource<T> CreateComposite<T>(
        IEnumerable<ICollectionDataSource<T>> dataSources,
        bool owns = false
    )
    {
        return new CompositeCollectionDataSource<T>(dataSources)
        {
            Owns = owns
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a <see cref="StaticCollectionDataSource{T}"/> that wraps the provided collection.
    /// The collection is materialized immediately into a <see cref="List{T}"/>.
    /// </remarks>
    public ICollectionDataSource<T> CreateStatic<T>(
        IEnumerable<T> collection,
        bool owns = true
    )
    {
        return new StaticCollectionDataSource<T>(
            collection,
            owns
        );
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates an <see cref="ObservableCollectionDataSource{T}"/> that wraps the provided
    /// observable collection and subscribes to its <see cref="ObservableCollection{T}.CollectionChanged"/> event.
    /// </remarks>
    public IDisposableCollectionDataSource<T> CreateObservable<T>(
        ObservableCollection<T> observableCollection,
        bool owns = true
    )
    {
        return new ObservableCollectionDataSource<T>(
            observableCollection,
            owns
        );
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a <see cref="PeriodicPollingCollectionDataSource{TItem,TState}"/> that periodically
    /// invokes the refresh delegate to fetch updated data. The polling timer starts immediately.
    /// </remarks>
    public IAsyncDisposableCollectionDataSource<TItem> CreatePeriodicPolling<TItem, TState>(
        TState state,
        IReadOnlyCollection<TItem> initialCollection,
        TimeSpan refreshInterval,
        RefreshCollectionAsyncDelegate<TItem, TState> refreshCollectionAsync,
        HandleExceptionAsyncDelegate? handleExceptionAsync = default
    )
    {
        return new PeriodicPollingCollectionDataSource<TItem, TState>(
            state,
            initialCollection,
            refreshInterval,
            refreshCollectionAsync,
            handleExceptionAsync
        );
    }
}
