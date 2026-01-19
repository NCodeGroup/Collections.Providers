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

namespace NCode.Collections.Providers;

/// <summary>
/// Defines an interface for objects that support change notification via <see cref="IChangeToken"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implementers of this interface provide a mechanism for consumers to observe when the underlying
/// data or state has changed. This is commonly used in scenarios where cached data needs to be
/// invalidated or refreshed when the source data changes.
/// </para>
/// <para>
/// The <see cref="IChangeToken"/> returned by <see cref="GetChangeToken"/> should be used for
/// a single change notification. Once the token signals a change, consumers should call
/// <see cref="GetChangeToken"/> again to obtain a new token for subsequent change notifications.
/// </para>
/// </remarks>
[PublicAPI]
public interface ISupportChangeToken
{
    /// <summary>
    /// Gets a <see cref="IChangeToken"/> that provides notifications when changes occur.
    /// </summary>
    /// <returns>
    /// An <see cref="IChangeToken"/> that can be used to register for change notifications.
    /// The token will signal when the underlying data or state has changed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each call to this method may return a new <see cref="IChangeToken"/> instance.
    /// After the returned token signals a change, callers should invoke this method again
    /// to obtain a fresh token for continued change observation.
    /// </para>
    /// <para>
    /// Implementations should never return <c>null</c>. If change notifications are not
    /// supported or not applicable, implementations should return a token that never signals
    /// (e.g., <see cref="INullChangeToken"/>).
    /// </para>
    /// </remarks>
    IChangeToken GetChangeToken();
}
