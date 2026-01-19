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
/// Represents an asynchronous method that is periodically invoked to refresh a collection from a remote source.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <typeparam name="TState">
/// The type of the state parameter. This can be used to provide context such as API clients,
/// database connections, configuration, or other dependencies needed during refresh.
/// </typeparam>
/// <param name="state">
/// The state instance provided when the periodic polling data source was created.
/// Use this to access dependencies without requiring closures.
/// </param>
/// <param name="current">
/// The current collection of items. This can be used to compare with newly fetched data
/// to determine if changes have occurred, or to implement incremental/delta updates.
/// </param>
/// <param name="cancellationToken">
/// A cancellation token that is triggered when the data source is being disposed.
/// Implementations should honor this token to allow graceful shutdown.
/// </param>
/// <returns>
/// A <see cref="ValueTask{T}"/> containing a <see cref="RefreshCollectionResult{TItem}"/>
/// that indicates whether the collection has changed. Use <see cref="RefreshCollectionResultFactory.Changed{TItem}"/>
/// when new data is available, or <see cref="RefreshCollectionResultFactory.Unchanged{TItem}"/> when the
/// data has not changed since the last refresh.
/// </returns>
/// <remarks>
/// <para>
/// This delegate is invoked by the periodic polling data source at regular intervals
/// specified by the refresh interval. The implementation should fetch data from the
/// external source (database, API, file system, etc.) and return a result indicating
/// whether the data has changed.
/// </para>
/// <para>
/// Implementations should be designed to handle transient failures gracefully. If an
/// exception is thrown, it will be passed to the <see cref="HandleExceptionAsyncDelegate"/>
/// if one was provided during data source creation.
/// </para>
/// <example>
/// <para>Example implementation:</para>
/// <code>
/// async ValueTask&lt;RefreshCollectionResult&lt;User&gt;&gt; RefreshUsersAsync(
///     ApiClient client,
///     IReadOnlyCollection&lt;User&gt; current,
///     CancellationToken cancellationToken)
/// {
///     var users = await client.GetUsersAsync(cancellationToken);
///
///     // Simple change detection based on count (implement your own logic)
///     if (users.Count != current.Count)
///     {
///         return RefreshCollectionResultFactory.Changed(users);
///     }
///
///     return RefreshCollectionResultFactory.Unchanged&lt;User&gt;();
/// }
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="RefreshCollectionResult{TItem}"/>
/// <seealso cref="RefreshCollectionResultFactory"/>
/// <seealso cref="HandleExceptionAsyncDelegate"/>
/// <seealso cref="ICollectionDataSourceFactory.CreatePeriodicPolling{TItem,TState}"/>
[PublicAPI]
public delegate ValueTask<RefreshCollectionResult<TItem>> RefreshCollectionAsyncDelegate<TItem, in TState>(
    TState state,
    IReadOnlyCollection<TItem> current,
    CancellationToken cancellationToken
);
