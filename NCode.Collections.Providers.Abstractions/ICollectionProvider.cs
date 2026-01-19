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
/// Provides the composition root (i.e. top-level collection) of <typeparamref name="TItem"/> instances by
/// aggregating multiple data sources and providing change notifications.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <typeparam name="TCollection">
/// The type of collection that contains the items. Must implement <see cref="IEnumerable{T}"/>
/// of <typeparamref name="TItem"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// This interface represents the top-level aggregation point for collection data sources.
/// It combines items from multiple <see cref="ICollectionDataSource{T}"/> instances into
/// a single unified collection, while providing consolidated change notifications.
/// </para>
/// <para>
/// The provider automatically subscribes to change tokens from all registered data sources
/// and propagates change notifications to consumers. When any underlying data source changes,
/// the provider's change token will signal, and consumers should re-read the <see cref="Collection"/>
/// property to obtain the updated aggregated collection.
/// </para>
/// <para>
/// This interface extends <see cref="IAsyncDisposable"/> to ensure proper cleanup of
/// subscriptions and any disposable data sources when the provider is no longer needed.
/// </para>
/// </remarks>
/// <seealso cref="ISupportChangeToken"/>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="ICollectionProviderFactory"/>
[PublicAPI]
public interface ICollectionProvider<out TItem, out TCollection> : ISupportChangeToken, IAsyncDisposable
    where TCollection : IEnumerable<TItem>
{
    /// <summary>
    /// Gets a read-only collection of <typeparamref name="TItem"/> instances.
    /// </summary>
    /// <value>
    /// A <typeparamref name="TCollection"/> containing all items aggregated from the
    /// registered data sources. This collection represents a snapshot of the current
    /// state and may change when the change token signals.
    /// </value>
    /// <remarks>
    /// <para>
    /// The returned collection is an aggregation of items from all registered
    /// <see cref="ICollectionDataSource{T}"/> instances. The order of items may depend
    /// on the order of data source registration and their individual collection orders.
    /// </para>
    /// <para>
    /// This property is thread-safe for concurrent reads. After receiving a change
    /// notification from <see cref="ISupportChangeToken.GetChangeToken"/>, consumers
    /// should re-read this property to obtain the updated collection.
    /// </para>
    /// </remarks>
    TCollection Collection { get; }
}
