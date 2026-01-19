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

namespace NCode.Collections.Providers.PeriodicPolling;

/// <summary>
/// Provides factory methods for creating instances of <see cref="RefreshCollectionResult{TItem}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides expressive factory methods for creating refresh results
/// in <see cref="RefreshCollectionAsyncDelegate{TItem,TState}"/> implementations.
/// Using these methods improves code readability compared to direct struct construction.
/// </para>
/// <example>
/// <para>Example usage in a refresh delegate:</para>
/// <code>
/// async Task&lt;RefreshCollectionResult&lt;MyItem&gt;&gt; RefreshAsync(
///     IReadOnlyCollection&lt;MyItem&gt; currentCollection,
///     MyState state,
///     CancellationToken cancellationToken)
/// {
///     var newData = await state.ApiClient.FetchDataAsync(cancellationToken);
///
///     if (HasChanges(currentCollection, newData))
///     {
///         return RefreshCollectionResultFactory.Changed(newData);
///     }
///
///     return RefreshCollectionResultFactory.Unchanged&lt;MyItem&gt;();
/// }
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="RefreshCollectionResult{TItem}"/>
/// <seealso cref="RefreshCollectionAsyncDelegate{TItem,TState}"/>
[PublicAPI]
public static class RefreshCollectionResultFactory
{
    /// <summary>
    /// Creates a <see cref="RefreshCollectionResult{TItem}"/> indicating that the collection has not changed.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the collection.</typeparam>
    /// <returns>
    /// A <see cref="RefreshCollectionResult{TItem}"/> where <see cref="RefreshCollectionResult{TItem}.IsUnchanged"/>
    /// is <c>true</c> and <see cref="RefreshCollectionResult{TItem}.NewCollection"/> is <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method when the refresh operation determines that the data has not changed
    /// since the last refresh. This allows the polling data source to skip unnecessary
    /// collection updates and change notifications.
    /// </para>
    /// </remarks>
    public static RefreshCollectionResult<TItem> Unchanged<TItem>() => default;

    /// <summary>
    /// Creates a <see cref="RefreshCollectionResult{TItem}"/> indicating that the collection has changed.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the collection.</typeparam>
    /// <param name="newCollection">
    /// The new collection containing the updated data. This collection will replace
    /// the current collection in the data source.
    /// </param>
    /// <returns>
    /// A <see cref="RefreshCollectionResult{TItem}"/> where <see cref="RefreshCollectionResult{TItem}.IsChanged"/>
    /// is <c>true</c> and <see cref="RefreshCollectionResult{TItem}.NewCollection"/> contains the provided collection.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method when the refresh operation has fetched new data that differs from
    /// the current collection. The polling data source will update its collection and
    /// signal a change notification to consumers.
    /// </para>
    /// </remarks>
    public static RefreshCollectionResult<TItem> Changed<TItem>(IReadOnlyCollection<TItem> newCollection) =>
        new(newCollection);
}
