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

using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace NCode.Collections.Providers.PeriodicPolling;

/// <summary>
/// Represents an asynchronous method that handles exceptions thrown during periodic collection refresh operations.
/// </summary>
/// <param name="exception">
/// An <see cref="ExceptionDispatchInfo"/> containing the captured exception. This preserves the original
/// stack trace and allows the handler to re-throw the exception if desired using
/// <see cref="ExceptionDispatchInfo.Throw()"/>.
/// </param>
/// <param name="cancellationToken">
/// A cancellation token that is triggered when the data source is being disposed.
/// Implementations should honor this token to allow graceful shutdown during exception handling.
/// </param>
/// <returns>
/// A <see cref="ValueTask"/> representing the asynchronous exception handling operation.
/// </returns>
/// <remarks>
/// <para>
/// This delegate is invoked when <see cref="RefreshCollectionAsyncDelegate{TItem,TState}"/> throws
/// an exception during a polling cycle. It provides an opportunity to log errors, send alerts,
/// implement retry logic, or perform other error handling operations.
/// </para>
/// <para>
/// If this delegate is not provided when creating a periodic polling data source, exceptions
/// are silently ignored and the existing collection is retained. This may be acceptable for
/// transient network failures but could hide persistent problems.
/// </para>
/// <para>
/// The handler should not re-throw the exception unless it wants to propagate the error.
/// After the handler completes (successfully or not), the polling timer continues and
/// the next refresh will be attempted after the configured interval.
/// </para>
/// <example>
/// <para>Example implementation with logging:</para>
/// <code>
/// async ValueTask HandleRefreshExceptionAsync(
///     ExceptionDispatchInfo exceptionInfo,
///     CancellationToken cancellationToken)
/// {
///     var exception = exceptionInfo.SourceException;
///     logger.LogError(exception, "Failed to refresh collection");
///
///     // Optionally send an alert for persistent failures
///     await alertService.SendAlertAsync(
///         $"Collection refresh failed: {exception.Message}",
///         cancellationToken);
/// }
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="RefreshCollectionAsyncDelegate{TItem,TState}"/>
/// <seealso cref="ICollectionDataSourceFactory.CreatePeriodicPolling{TItem,TState}"/>
/// <seealso cref="ExceptionDispatchInfo"/>
[PublicAPI]
public delegate ValueTask HandleExceptionAsyncDelegate(
    ExceptionDispatchInfo exception,
    CancellationToken cancellationToken
);
