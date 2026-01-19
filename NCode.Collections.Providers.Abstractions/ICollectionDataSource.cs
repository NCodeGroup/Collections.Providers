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
/// Provides a collection of <typeparamref name="T"/> instances and notifications when changes occur.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This interface represents a data source that exposes a collection of items along with
/// change notification capabilities via <see cref="ISupportChangeToken"/>. It serves as
/// the fundamental building block for the collection provider pattern.
/// </para>
/// <para>
/// Concrete implementations should be registered in dependency injection using the
/// <see cref="ICollectionDataSource{T}"/> closed generic interface. Multiple data sources
/// of the same type can be registered and will be aggregated by <c>ICollectionProvider</c>.
/// </para>
/// <para>
/// Implementations can optionally implement <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>
/// to dispose of any resources. Consider using <see cref="IDisposableCollectionDataSource{T}"/> or
/// <see cref="IAsyncDisposableCollectionDataSource{T}"/> for explicit lifecycle management.
/// </para>
/// </remarks>
/// <seealso cref="ISupportChangeToken"/>
/// <seealso cref="IDisposableCollectionDataSource{T}"/>
/// <seealso cref="IAsyncDisposableCollectionDataSource{T}"/>
[PublicAPI]
public interface ICollectionDataSource<out T> : ISupportChangeToken
{
    /// <summary>
    /// Gets a read-only collection of <typeparamref name="T"/> instances.
    /// </summary>
    /// <value>
    /// An <see cref="IEnumerable{T}"/> containing the current items in the data source.
    /// This collection should be treated as a snapshot and may change when the
    /// change token signals.
    /// </value>
    /// <remarks>
    /// <para>
    /// The returned collection represents the current state of the data source.
    /// Consumers should re-read this property after receiving a change notification
    /// from <see cref="ISupportChangeToken.GetChangeToken"/> to obtain the updated collection.
    /// </para>
    /// <para>
    /// Implementations should ensure this property is thread-safe for concurrent reads.
    /// The collection itself should not be modified after being returned.
    /// </para>
    /// </remarks>
    IEnumerable<T> Collection { get; }
}

/// <summary>
/// Provides a collection of <typeparamref name="T"/> instances with synchronous disposal support.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="ICollectionDataSource{T}"/> with <see cref="IDisposable"/>
/// for data sources that require explicit resource cleanup, such as file handles, database
/// connections, or event subscriptions.
/// </para>
/// <para>
/// Use this interface when the data source manages resources that must be released
/// synchronously when the data source is no longer needed.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="IAsyncDisposableCollectionDataSource{T}"/>
[PublicAPI]
public interface IDisposableCollectionDataSource<out T> : ICollectionDataSource<T>, IDisposable
{
    // nothing
}

/// <summary>
/// Provides a collection of <typeparamref name="T"/> instances with asynchronous disposal support.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="ICollectionDataSource{T}"/> with <see cref="IAsyncDisposable"/>
/// for data sources that require asynchronous resource cleanup, such as network connections,
/// async file operations, or cloud service clients.
/// </para>
/// <para>
/// Use this interface when the data source manages resources that benefit from or require
/// asynchronous disposal when the data source is no longer needed.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="IDisposableCollectionDataSource{T}"/>
[PublicAPI]
public interface IAsyncDisposableCollectionDataSource<out T> : ICollectionDataSource<T>, IAsyncDisposable
{
    // nothing
}
