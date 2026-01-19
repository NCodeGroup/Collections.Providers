#region Copyright Preamble

//
//    Copyright @ 2023 NCode Group
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

namespace NCode.Collections.Providers;

/// <summary>
/// Provides a singleton implementation of <see cref="INullChangeToken"/> that never generates any change notifications.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the "null object" pattern for <see cref="IChangeToken"/>, providing a
/// safe, reusable token that can be returned when change notifications are not supported or
/// not applicable. Using this class avoids the need for null checks in consuming code.
/// </para>
/// <para>
/// Use the <see cref="Singleton"/> property to access the shared instance rather than
/// creating new instances, as all instances behave identically.
/// </para>
/// </remarks>
/// <seealso cref="INullChangeToken"/>
/// <seealso cref="IChangeToken"/>
/// <seealso cref="ISupportChangeToken"/>
[PublicAPI]
public sealed class NullChangeToken : INullChangeToken
{
    /// <summary>
    /// Gets a singleton instance of <see cref="NullChangeToken"/>.
    /// </summary>
    /// <value>
    /// A shared <see cref="NullChangeToken"/> instance that can be reused throughout the application.
    /// </value>
    /// <remarks>
    /// This singleton should be used instead of creating new instances, as all
    /// <see cref="NullChangeToken"/> instances are functionally identical.
    /// </remarks>
    public static NullChangeToken Singleton { get; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// Always returns <c>false</c> because this token never signals changes.
    /// </remarks>
    public bool HasChanged => false;

    /// <inheritdoc />
    /// <remarks>
    /// Always returns <c>false</c> because this token does not support active change callbacks.
    /// </remarks>
    public bool ActiveChangeCallbacks => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns an empty disposable that performs no action when disposed. The provided
    /// <paramref name="callback"/> will never be invoked since this token never signals changes.
    /// </remarks>
    public IDisposable RegisterChangeCallback(Action<object> callback, object? state) => Disposable.Empty;
}
