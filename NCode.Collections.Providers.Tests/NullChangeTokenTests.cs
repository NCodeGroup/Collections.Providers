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

using Microsoft.Extensions.Primitives;

namespace NCode.Collections.Providers.Tests;

public class NullChangeTokenTests
{
    #region Singleton Tests

    [Fact]
    public void Singleton_ReturnsNonNullInstance()
    {
        var singleton = NullChangeToken.Singleton;

        Assert.NotNull(singleton);
    }

    [Fact]
    public void Singleton_ReturnsSameInstanceOnMultipleCalls()
    {
        var first = NullChangeToken.Singleton;
        var second = NullChangeToken.Singleton;

        Assert.Same(first, second);
    }

    #endregion

    #region HasChanged Tests

    [Fact]
    public void HasChanged_ReturnsFalse()
    {
        var token = NullChangeToken.Singleton;

        Assert.False(token.HasChanged);
    }

    [Fact]
    public void HasChanged_AlwaysReturnsFalse_AfterMultipleAccesses()
    {
        var token = NullChangeToken.Singleton;

        _ = token.HasChanged;
        _ = token.HasChanged;
        _ = token.HasChanged;

        Assert.False(token.HasChanged);
    }

    #endregion

    #region ActiveChangeCallbacks Tests

    [Fact]
    public void ActiveChangeCallbacks_ReturnsFalse()
    {
        var token = NullChangeToken.Singleton;

        Assert.False(token.ActiveChangeCallbacks);
    }

    #endregion

    #region RegisterChangeCallback Tests

    [Fact]
    public void RegisterChangeCallback_ReturnsNonNullDisposable()
    {
        var token = NullChangeToken.Singleton;

        var disposable = token.RegisterChangeCallback(_ => { }, null);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void RegisterChangeCallback_DisposableCanBeDisposedWithoutException()
    {
        var token = NullChangeToken.Singleton;
        var disposable = token.RegisterChangeCallback(_ => { }, null);

        var exception = Record.Exception(() => disposable.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void RegisterChangeCallback_DisposableCanBeDisposedMultipleTimes()
    {
        var token = NullChangeToken.Singleton;
        var disposable = token.RegisterChangeCallback(_ => { }, null);

        var exception = Record.Exception(() =>
        {
            disposable.Dispose();
            disposable.Dispose();
            disposable.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void RegisterChangeCallback_NeverInvokesCallback()
    {
        var token = NullChangeToken.Singleton;
        var callbackInvoked = false;

        token.RegisterChangeCallback(_ => callbackInvoked = true, null);

        Assert.False(callbackInvoked);
    }

    [Fact]
    public void RegisterChangeCallback_WithNullState_ReturnsDisposable()
    {
        var token = NullChangeToken.Singleton;

        var disposable = token.RegisterChangeCallback(_ => { }, null);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void RegisterChangeCallback_WithNonNullState_ReturnsDisposable()
    {
        var token = NullChangeToken.Singleton;
        const string state = "test state";

        var disposable = token.RegisterChangeCallback(_ => { }, state);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void RegisterChangeCallback_MultipleRegistrations_AllReturnDisposables()
    {
        var token = NullChangeToken.Singleton;

        var disposable1 = token.RegisterChangeCallback(_ => { }, null);
        var disposable2 = token.RegisterChangeCallback(_ => { }, null);
        var disposable3 = token.RegisterChangeCallback(_ => { }, null);

        Assert.NotNull(disposable1);
        Assert.NotNull(disposable2);
        Assert.NotNull(disposable3);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void NullChangeToken_ImplementsINullChangeToken()
    {
        var token = NullChangeToken.Singleton;

        Assert.IsAssignableFrom<INullChangeToken>(token);
    }

    [Fact]
    public void NullChangeToken_ImplementsIChangeToken()
    {
        var token = NullChangeToken.Singleton;

        Assert.IsAssignableFrom<IChangeToken>(token);
    }

    [Fact]
    public void NullChangeToken_CanBeUsedAsIChangeToken()
    {
        IChangeToken token = NullChangeToken.Singleton;

        Assert.False(token.HasChanged);
        Assert.False(token.ActiveChangeCallbacks);
        Assert.NotNull(token.RegisterChangeCallback(_ => { }, null));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Singleton_IsThreadSafe()
    {
        const int taskCount = 100;

        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(() => NullChangeToken.Singleton))
            .ToArray();

        var tokens = await Task.WhenAll(tasks);

        Assert.All(tokens, t => Assert.Same(NullChangeToken.Singleton, t));
    }

    [Fact]
    public void RegisterChangeCallback_IsThreadSafe()
    {
        var token = NullChangeToken.Singleton;
        const int iterationCount = 1000;
        var disposables = new IDisposable[iterationCount];

        var exception = Record.Exception(() =>
        {
            Parallel.For(0, iterationCount, i =>
            {
                disposables[i] = token.RegisterChangeCallback(_ => { }, i);
            });
        });

        Assert.Null(exception);
        Assert.All(disposables, d => Assert.NotNull(d));
    }

    #endregion
}
