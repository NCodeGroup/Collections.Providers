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

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NCode.Collections.Providers.PeriodicPolling;

/// <summary>
/// Represents the result of a collection refresh operation for periodic polling data sources.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This struct is returned by <see cref="RefreshCollectionAsyncDelegate{TItem,TState}"/> to indicate
/// whether the collection has changed during a polling operation. Using a result struct instead of
/// always returning a collection allows implementations to optimize for the common case where
/// data has not changed.
/// </para>
/// <para>
/// Use <see cref="RefreshCollectionResultFactory.Changed{TItem}"/> to create a result indicating
/// the collection has changed, or <see cref="RefreshCollectionResultFactory.Unchanged{TItem}"/>
/// to indicate no changes occurred.
/// </para>
/// </remarks>
/// <seealso cref="RefreshCollectionResultFactory"/>
/// <seealso cref="RefreshCollectionAsyncDelegate{TItem,TState}"/>
[PublicAPI]
public readonly struct RefreshCollectionResult<TItem>
{
    /// <summary>
    /// Gets a value indicating whether the collection has changed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the refresh operation detected changes and <see cref="NewCollection"/>
    /// contains the updated collection; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// When this property returns <c>true</c>, <see cref="NewCollection"/> is guaranteed to be non-null.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(NewCollection))]
    public bool IsChanged => NewCollection != null;

    /// <summary>
    /// Gets a value indicating whether the collection has not changed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the refresh operation did not detect any changes and the existing
    /// collection should be retained; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This is the logical inverse of <see cref="IsChanged"/> and is provided for readability
    /// in conditional expressions.
    /// </remarks>
    [MemberNotNullWhen(false, nameof(NewCollection))]
    public bool IsUnchanged => !IsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshCollectionResult{TItem}"/> struct with a new collection.
    /// </summary>
    /// <param name="newCollection">
    /// The new collection representing the updated data. Must not be <c>null</c> when indicating
    /// a change has occurred.
    /// </param>
    /// <remarks>
    /// <para>
    /// Prefer using <see cref="RefreshCollectionResultFactory.Changed{TItem}"/> instead of calling
    /// this constructor directly, as it provides a more expressive API.
    /// </para>
    /// </remarks>
    public RefreshCollectionResult(IReadOnlyCollection<TItem> newCollection)
    {
        NewCollection = newCollection;
    }

    /// <summary>
    /// Gets the new collection if the collection has changed; otherwise, <c>null</c>.
    /// </summary>
    /// <value>
    /// An <see cref="IReadOnlyCollection{T}"/> containing the updated items when <see cref="IsChanged"/>
    /// is <c>true</c>; otherwise, <c>null</c> indicating the existing collection should be retained.
    /// </value>
    public IReadOnlyCollection<TItem>? NewCollection { get; }
}
