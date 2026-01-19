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
using Microsoft.Extensions.Primitives;
using NCode.Disposables;

namespace NCode.Collections.Providers.DataSources;

/// <summary>
/// Provides an implementation of <see cref="ICollectionDataSource{T}"/> that wraps a static,
/// immutable collection without change notifications.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This data source is intended for collections that are known at initialization time and
/// will not change during the application lifetime. The change token returned by
/// <see cref="GetChangeToken"/> is always <see cref="NullChangeToken.Singleton"/>, which
/// never signals changes.
/// </para>
/// <para>
/// The provided collection is materialized into a <see cref="List{T}"/> during construction
/// to ensure it is captured as a snapshot and can be safely enumerated multiple times.
/// </para>
/// <para>
/// This is ideal for configuration data, reference data, or any collection that is loaded
/// once and remains constant.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="NullChangeToken"/>
/// <seealso cref="ICollectionDataSourceFactory.CreateStatic{T}"/>
[PublicAPI]
public sealed class StaticCollectionDataSource<T> : ICollectionDataSource<T>, IDisposable
{
    private bool IsDisposed { get; set; }
    private bool Owns { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the static collection provided during construction. This collection is
    /// materialized as a <see cref="List{T}"/> and will not change.
    /// </remarks>
    public IEnumerable<T> Collection { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticCollectionDataSource{T}"/> class
    /// with the specified collection.
    /// </summary>
    /// <param name="collection">
    /// The collection of items to wrap. This collection is enumerated immediately and
    /// materialized into a <see cref="List{T}"/> to capture a snapshot.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the data source takes ownership of the collection items (not the
    /// collection itself) and will dispose any items that implement <see cref="IDisposable"/>
    /// when the data source is disposed. When <c>false</c>, the caller retains ownership.
    /// Defaults to <c>true</c>.
    /// </param>
    public StaticCollectionDataSource(IEnumerable<T> collection, bool owns = true)
    {
        Owns = owns;
        Collection = collection.ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// When <see cref="Owns"/> is <c>true</c> and the items implement <see cref="IDisposable"/>,
    /// all items in the collection are disposed. The collection itself is not disposed.
    /// </para>
    /// <para>
    /// This method is idempotent and can be called multiple times safely.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (!Owns || IsDisposed) return;
        IsDisposed = true;

        var disposableCollection = Collection as IEnumerable<IDisposable>;
        disposableCollection?.DisposeAll();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Always returns <see cref="NullChangeToken.Singleton"/> because this data source
    /// represents static data that never changes.
    /// </remarks>
    public IChangeToken GetChangeToken() => NullChangeToken.Singleton;
}
