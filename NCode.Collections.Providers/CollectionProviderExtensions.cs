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
using NCode.Collections.Providers.DataSources;

namespace NCode.Collections.Providers;

/// <summary>
/// Provides extension methods for the <see cref="ICollectionProvider{TItem,TCollection}"/> abstraction.
/// </summary>
/// <remarks>
/// These extensions enable composition and adaptation patterns, allowing collection providers
/// to be used in contexts that expect data sources.
/// </remarks>
/// <seealso cref="ICollectionProvider{TItem,TCollection}"/>
/// <seealso cref="ICollectionDataSource{T}"/>
[PublicAPI]
public static class CollectionProviderExtensions
{
    extension<TItem>(ICollectionProvider<TItem, IEnumerable<TItem>> provider)
    {
        /// <summary>
        /// Adapts an <see cref="ICollectionProvider{TItem,TCollection}"/> to an <see cref="ICollectionDataSource{TItem}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollectionDataSource{TItem}"/> that wraps the provider, exposing its collection
        /// and change token through the data source interface.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method enables hierarchical composition by allowing a collection provider to be
        /// used as a data source for another provider. This is useful when building layered
        /// collection architectures.
        /// </para>
        /// <para>
        /// The returned data source does not implement <see cref="IDisposable"/> or
        /// <see cref="IAsyncDisposable"/> because the original provider controls the lifetime
        /// of the underlying resources. Dispose the provider directly when finished.
        /// </para>
        /// </remarks>
        public ICollectionDataSource<TItem> AsDataSource()
        {
            return new CollectionDataSourceAdapter<TItem>(provider);
        }
    }
}
