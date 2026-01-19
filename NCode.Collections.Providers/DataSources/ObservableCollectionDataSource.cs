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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using NCode.Disposables;

#if NET10_0_OR_GREATER
using LockAlias = System.Threading.Lock;
#else
using LockAlias = object;
#endif

namespace NCode.Collections.Providers.DataSources;

/// <summary>
/// Provides an implementation of <see cref="ICollectionDataSource{T}"/> that wraps an
/// <see cref="ObservableCollection{T}"/> and provides automatic change notifications.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This data source subscribes to the <see cref="ObservableCollection{T}.CollectionChanged"/> event
/// and signals its change token whenever the underlying collection is modified (items added, removed,
/// replaced, moved, or cleared).
/// </para>
/// <para>
/// This is ideal for scenarios where you want to expose a mutable collection that can be modified
/// at runtime, with automatic propagation of changes to any subscribed consumers.
/// </para>
/// <para>
/// When <see cref="Dispose"/> is called, the event subscription is removed. If the data source owns
/// the collection items and they implement <see cref="IDisposable"/>, they will also be disposed.
/// </para>
/// </remarks>
/// <seealso cref="ICollectionDataSource{T}"/>
/// <seealso cref="ObservableCollection{T}"/>
/// <seealso cref="ICollectionDataSourceFactory.CreateObservable{T}"/>
[PublicAPI]
public sealed class ObservableCollectionDataSource<T> : IDisposableCollectionDataSource<T>
{
    private bool Owns { get; }
    private LockAlias SyncObj { get; } = new();
    private bool IsDisposed { get; set; }
    private CancellationTokenSource? ChangeTokenSource { get; set; }
    private CancellationChangeToken? ConsumerChangeToken { get; set; }
    private ObservableCollection<T> ObservableCollection { get; }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Returns the underlying <see cref="ObservableCollection{T}"/> directly. Unlike other data source
    /// implementations, this collection is mutable and changes made to it will trigger change notifications.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the data source has been disposed.</exception>
    public IEnumerable<T> Collection
    {
        get
        {
            ThrowIfDisposed();
            return ObservableCollection;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableCollectionDataSource{T}"/> class
    /// with the specified observable collection.
    /// </summary>
    /// <param name="observableCollection">
    /// The <see cref="ObservableCollection{T}"/> to wrap. The data source subscribes to its
    /// <see cref="ObservableCollection{T}.CollectionChanged"/> event to detect changes.
    /// </param>
    /// <param name="owns">
    /// When <c>true</c>, the data source takes ownership of the collection items (not the collection itself)
    /// and will dispose any items that implement <see cref="IDisposable"/> when the data source is disposed.
    /// When <c>false</c>, the caller retains ownership of the items. Defaults to <c>true</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// The data source immediately subscribes to the <see cref="INotifyCollectionChanged.CollectionChanged"/>
    /// event upon construction. The subscription is removed when the data source is disposed.
    /// </para>
    /// </remarks>
    public ObservableCollectionDataSource(ObservableCollection<T> observableCollection, bool owns = true)
    {
        Owns = owns;
        ObservableCollection = observableCollection;
        ObservableCollection.CollectionChanged += HandleChange;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The change token is lazily initialized on first access. The returned token signals when
    /// the underlying <see cref="ObservableCollection{T}"/> raises its <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
    /// </para>
    /// <para>
    /// Consumers should call this method again after receiving a change notification to
    /// obtain a new token for subsequent changes.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the data source has been disposed.</exception>
    public IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return ConsumerChangeToken;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    /// <inheritdoc />
    public void Dispose()
    {
        if (IsDisposed) return;

        List<IDisposable>? disposables;

        lock (SyncObj)
        {
            if (IsDisposed) return;
            IsDisposed = true;

            ObservableCollection.CollectionChanged -= HandleChange;

            disposables = [];

            if (Owns && ObservableCollection is IEnumerable<IDisposable> disposableCollection)
            {
                disposables.AddRange(disposableCollection);
            }

            if (ChangeTokenSource is not null)
            {
                disposables.Add(ChangeTokenSource);
            }

            ChangeTokenSource = null;
            ConsumerChangeToken = null;
        }

        disposables.DisposeAll();
    }

    [MemberNotNull(nameof(ConsumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        if (ConsumerChangeToken is not null) return;
        lock (SyncObj)
        {
            if (ConsumerChangeToken is not null) return;

            ThrowIfDisposed();
            RefreshChangeToken();
        }
    }

    private void HandleChange(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (IsDisposed) return;

        CancellationTokenSource? oldTokenSource = null;
        try
        {
            lock (SyncObj)
            {
                if (IsDisposed) return;

                oldTokenSource = ChangeTokenSource;

                ChangeTokenSource = null;
                ConsumerChangeToken = null;

                // refresh the cached change token
                if (oldTokenSource is not null)
                {
                    RefreshChangeToken();
                }
            }

            oldTokenSource?.Cancel();
        }
        finally
        {
            oldTokenSource?.Dispose();
        }
    }

    [MemberNotNull(nameof(ConsumerChangeToken))]
    private void RefreshChangeToken()
    {
        ChangeTokenSource = new CancellationTokenSource();
        ConsumerChangeToken = new CancellationChangeToken(ChangeTokenSource.Token);
    }
}
