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

using JetBrains.Annotations;

namespace NCode.Collections.Providers;

/// <summary>
/// Provides the ability to create instances of <see cref="ICollectionProvider{TItem,TCollection}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This factory is the primary entry point for creating collection providers. It allows
/// constructing providers from either a single data source or multiple data sources,
/// with control over ownership semantics for resource management.
/// </para>
/// <para>
/// The factory should be registered as a singleton in dependency injection and can be
/// used to create providers on-demand when the set of data sources needs to be
/// determined at runtime.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionProvider{TItem,TCollection}"/>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="ICollectionDataSourceFactory"/>
[PublicAPI]
public interface ICollectionProviderFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="ICollectionProvider{TItem,TCollection}"/> using a single data source.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the collection.</typeparam>
    /// <typeparam name="TCollection">
    /// The type of collection to create. Must implement <see cref="IEnumerable{T}"/>
    /// of <typeparamref name="TItem"/>.
    /// </typeparam>
    /// <param name="collectionFactory">
    /// A factory method that creates the output collection from an enumerable of items.
    /// This is called whenever the underlying data changes to produce a new collection snapshot.
    /// </param>
    /// <param name="dataSource">The data source that provides items for the collection.</param>
    /// <param name="owns">
    /// When <c>true</c>, the collection provider takes ownership of the data source and will
    /// dispose it when the provider is disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    /// <returns>
    /// A new <see cref="ICollectionProvider{TItem,TCollection}"/> instance that aggregates
    /// items from the specified data source and provides change notifications.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload is useful when you have a single data source and want to wrap it
    /// in a provider for consistent access patterns and change notification handling.
    /// </para>
    /// </remarks>
    ICollectionProvider<TItem, TCollection> Create<TItem, TCollection>(
        Func<IEnumerable<TItem>, TCollection> collectionFactory,
        ICollectionDataSource<TItem> dataSource,
        bool owns = true
    )
        where TCollection : IEnumerable<TItem>;

    /// <summary>
    /// Creates a new instance of <see cref="ICollectionProvider{TItem,TCollection}"/> using multiple data sources.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the collection.</typeparam>
    /// <typeparam name="TCollection">
    /// The type of collection to create. Must implement <see cref="IEnumerable{T}"/>
    /// of <typeparamref name="TItem"/>.
    /// </typeparam>
    /// <param name="collectionFactory">
    /// A factory method that creates the output collection from an enumerable of items.
    /// This is called whenever any underlying data source changes to produce a new
    /// aggregated collection snapshot.
    /// </param>
    /// <param name="dataSources">
    /// The collection of data sources whose items will be aggregated into a single collection.
    /// Items from all data sources are combined in enumeration order.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the collection provider takes ownership of all data sources and will
    /// dispose them when the provider is disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>false</c>.
    /// </param>
    /// <returns>
    /// A new <see cref="ICollectionProvider{TItem,TCollection}"/> instance that aggregates
    /// items from all specified data sources and provides consolidated change notifications.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload is the primary method for aggregating multiple data sources into a
    /// unified collection. The provider subscribes to change tokens from all data sources
    /// and signals its own change token when any source changes.
    /// </para>
    /// <para>
    /// Note that the default value for <paramref name="owns"/> differs between overloads:
    /// this overload defaults to <c>false</c> because data sources are typically managed
    /// by the dependency injection container when multiple sources are involved.
    /// </para>
    /// </remarks>
    ICollectionProvider<TItem, TCollection> Create<TItem, TCollection>(
        Func<IEnumerable<TItem>, TCollection> collectionFactory,
        IEnumerable<ICollectionDataSource<TItem>> dataSources,
        bool owns = false
    )
        where TCollection : IEnumerable<TItem>;
}
