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
/// Provides a default implementation for the <see cref="ICollectionProvider{TItem,TCollection}"/> abstraction
/// using a factory delegate to create collections.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <typeparam name="TCollection">
/// The type of collection that contains the items. Must implement <see cref="IEnumerable{T}"/>
/// of <typeparamref name="TItem"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="BaseCollectionProvider{TItem,TCollection}"/> by accepting a
/// factory delegate that creates the collection. This allows for flexible collection creation
/// without requiring a custom derived class.
/// </para>
/// <para>
/// For most use cases, instances should be created via <see cref="ICollectionProviderFactory"/>
/// rather than instantiating this class directly.
/// </para>
/// </remarks>
/// <seealso cref="BaseCollectionProvider{TItem,TCollection}"/>
/// <seealso cref="ICollectionProviderFactory"/>
[PublicAPI]
public class CollectionProvider<TItem, TCollection> : BaseCollectionProvider<TItem, TCollection>
    where TCollection : IEnumerable<TItem>
{
    private Func<IEnumerable<TItem>, TCollection> CollectionFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionProvider{TItem,TCollection}"/> class
    /// with a single data source.
    /// </summary>
    /// <param name="collectionFactory">
    /// A factory delegate that creates the <typeparamref name="TCollection"/> from an enumerable of items.
    /// This delegate is invoked during initial collection creation and whenever the data source changes.
    /// </param>
    /// <param name="dataSource">The <see cref="ICollectionDataSource{T}"/> instance that provides items for the collection.</param>
    /// <param name="owns">
    /// When <c>true</c>, the provider takes ownership of the data source and will dispose it
    /// when the provider is disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    public CollectionProvider(
        Func<IEnumerable<TItem>, TCollection> collectionFactory,
        ICollectionDataSource<TItem> dataSource,
        bool owns = true
    )
        : base(dataSource, owns)
    {
        CollectionFactory = collectionFactory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionProvider{TItem,TCollection}"/> class
    /// with multiple data sources that are aggregated into a single collection.
    /// </summary>
    /// <param name="collectionFactory">
    /// A factory delegate that creates the <typeparamref name="TCollection"/> from an enumerable of items.
    /// This delegate is invoked during initial collection creation and whenever any data source changes.
    /// </param>
    /// <param name="dataSources">
    /// A collection of <see cref="ICollectionDataSource{T}"/> instances whose items will be
    /// aggregated into a single collection.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the provider takes ownership of the individual data sources and will
    /// dispose them when the provider is disposed. When <c>false</c>, the caller (typically the
    /// DI container) retains ownership. Defaults to <c>false</c>.
    /// </param>
    public CollectionProvider(
        Func<IEnumerable<TItem>, TCollection> collectionFactory,
        IEnumerable<ICollectionDataSource<TItem>> dataSources,
        bool owns = false
    )
        : base(dataSources, owns)
    {
        CollectionFactory = collectionFactory;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Invokes the factory delegate provided in the constructor to create the collection.
    /// </remarks>
    protected override TCollection CreateCollection(IEnumerable<TItem> items)
    {
        return CollectionFactory(items);
    }
}
