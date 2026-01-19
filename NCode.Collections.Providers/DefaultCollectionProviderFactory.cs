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
/// Provides the default implementation of the <see cref="ICollectionProviderFactory"/> abstraction.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates instances of <see cref="CollectionProvider{TItem,TCollection}"/>, which
/// aggregates items from one or more data sources into a unified collection with consolidated
/// change notifications.
/// </para>
/// <para>
/// This class should be registered as a singleton in dependency injection. It is stateless
/// and thread-safe.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionProviderFactory"/>
/// <seealso cref="CollectionProvider{TItem,TCollection}"/>
[PublicAPI]
public class DefaultCollectionProviderFactory : ICollectionProviderFactory
{
    /// <inheritdoc />
    /// <remarks>
    /// Creates a <see cref="CollectionProvider{TItem,TCollection}"/> that wraps a single data source.
    /// </remarks>
    public ICollectionProvider<TItem, TCollection> Create<TItem, TCollection>(
        Func<IEnumerable<TItem>, TCollection> collectionFactory,
        ICollectionDataSource<TItem> dataSource,
        bool owns = true
    )
        where TCollection : IEnumerable<TItem>
    {
        return new CollectionProvider<TItem, TCollection>(
            collectionFactory,
            dataSource,
            owns
        );
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a <see cref="CollectionProvider{TItem,TCollection}"/> that aggregates multiple
    /// data sources into a single collection with consolidated change notifications.
    /// </remarks>
    public ICollectionProvider<TItem, TCollection> Create<TItem, TCollection>(
        Func<IEnumerable<TItem>, TCollection> collectionFactory,
        IEnumerable<ICollectionDataSource<TItem>> dataSources,
        bool owns = false
    )
        where TCollection : IEnumerable<TItem>
    {
        return new CollectionProvider<TItem, TCollection>(
            collectionFactory,
            dataSources,
            owns
        );
    }
}
