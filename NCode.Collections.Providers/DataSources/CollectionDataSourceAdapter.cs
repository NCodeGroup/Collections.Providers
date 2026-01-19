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

using Microsoft.Extensions.Primitives;

namespace NCode.Collections.Providers.DataSources;

/// <summary>
/// Adapts an <see cref="ICollectionProvider{TItem,TCollection}"/> to the <see cref="ICollectionDataSource{T}"/> interface.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <param name="provider">The collection provider to adapt.</param>
/// <remarks>
/// <para>
/// This adapter enables hierarchical composition by allowing a collection provider to be
/// used as a data source for another provider. The adapter delegates all operations to
/// the underlying provider.
/// </para>
/// <para>
/// This class intentionally does not implement <see cref="IDisposable"/> or
/// <see cref="IAsyncDisposable"/> because the original provider is responsible for
/// managing the lifetime of the underlying resources. Consumers should dispose the
/// provider directly when finished.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="ICollectionProvider{TItem,TCollection}"/>
/// <seealso cref="CollectionProviderExtensions"/>
internal class CollectionDataSourceAdapter<TItem>(
    ICollectionProvider<TItem, IEnumerable<TItem>> provider
) : ICollectionDataSource<TItem>
{
    private ICollectionProvider<TItem, IEnumerable<TItem>> Provider { get; } = provider;

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="ISupportChangeToken.GetChangeToken"/> on the underlying provider.
    /// </remarks>
    public IChangeToken GetChangeToken() => Provider.GetChangeToken();

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="ICollectionProvider{TItem,TCollection}.Collection"/> on the underlying provider.
    /// </remarks>
    public IEnumerable<TItem> Collection => Provider.Collection;
}
