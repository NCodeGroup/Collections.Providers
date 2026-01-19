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
/// Represents a change token that never generates any change notifications.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IChangeToken"/> to represent a "null object" pattern
/// for change tokens. Implementations of this interface should:
/// </para>
/// <list type="bullet">
///   <item><description>Return <c>false</c> for <see cref="IChangeToken.HasChanged"/>.</description></item>
///   <item><description>Return <c>false</c> for <see cref="IChangeToken.ActiveChangeCallbacks"/>.</description></item>
///   <item><description>Return a no-op disposable from <see cref="IChangeToken.RegisterChangeCallback"/>.</description></item>
/// </list>
/// <para>
/// Use this interface when an <see cref="IChangeToken"/> is required but change notifications
/// are not supported or not applicable. This avoids the need for null checks and provides
/// a consistent behavior for consumers expecting a valid token.
/// </para>
/// </remarks>
/// <seealso cref="IChangeToken"/>
/// <seealso cref="ISupportChangeToken"/>
[PublicAPI]
public interface INullChangeToken : IChangeToken
{
    // nothing
}
