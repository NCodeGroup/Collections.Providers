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
using Microsoft.Extensions.Primitives;
using Moq;
using NCode.Collections.Providers.DataSources;

namespace NCode.Collections.Providers.Tests.DataSources;

public class CollectionDataSourceAdapterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidProvider_CreatesAdapter()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([1, 2, 3]);
        mockProvider.Setup(p => p.GetChangeToken()).Returns(NullChangeToken.Singleton);

        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        Assert.NotNull(adapter);
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public void Collection_DelegatesToProvider()
    {
        IEnumerable<int> expectedCollection = [1, 2, 3];
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns(expectedCollection);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        var collection = adapter.Collection;

        Assert.Same(expectedCollection, collection);
        mockProvider.Verify(p => p.Collection, Times.Once);
    }

    [Fact]
    public void Collection_ReturnsProviderCollection()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([1, 2, 3]);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        var collection = adapter.Collection;

        Assert.Equal([1, 2, 3], collection);
    }

    [Fact]
    public void Collection_ReturnsEmptyCollection_WhenProviderCollectionIsEmpty()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([]);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        var collection = adapter.Collection;

        Assert.Empty(collection);
    }

    [Fact]
    public void Collection_ReflectsProviderChanges()
    {
        var items = new List<int> { 1, 2, 3 };
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns(() => items);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        var initialCollection = adapter.Collection.ToList();
        items.Clear();
        items.AddRange([4, 5, 6]);
        var updatedCollection = adapter.Collection.ToList();

        Assert.Equal([1, 2, 3], initialCollection);
        Assert.Equal([4, 5, 6], updatedCollection);
    }

    #endregion

    #region GetChangeToken Tests

    [Fact]
    public void GetChangeToken_DelegatesToProvider()
    {
        var expectedToken = new CancellationChangeToken(CancellationToken.None);
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.GetChangeToken()).Returns(expectedToken);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        var token = adapter.GetChangeToken();

        Assert.Same(expectedToken, token);
        mockProvider.Verify(p => p.GetChangeToken(), Times.Once);
    }

    [Fact]
    public void GetChangeToken_ReturnsProviderChangeToken()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        var token = adapter.GetChangeToken();

        Assert.Same(NullChangeToken.Singleton, token);
    }

    [Fact]
    public void GetChangeToken_CalledMultipleTimes_DelegatesToProviderEachTime()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        _ = adapter.GetChangeToken();
        _ = adapter.GetChangeToken();
        _ = adapter.GetChangeToken();

        mockProvider.Verify(p => p.GetChangeToken(), Times.Exactly(3));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Adapter_ImplementsICollectionDataSource()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([]);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        Assert.IsAssignableFrom<ICollectionDataSource<int>>(adapter);
    }

    [Fact]
    public void Adapter_ImplementsISupportChangeToken()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([]);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        Assert.IsAssignableFrom<ISupportChangeToken>(adapter);
    }

    [Fact]
    public void Adapter_DoesNotImplementIDisposable()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([]);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        Assert.False(adapter is IDisposable);
    }

    [Fact]
    public void Adapter_DoesNotImplementIAsyncDisposable()
    {
        var mockProvider = new Mock<ICollectionProvider<int, IEnumerable<int>>>();
        mockProvider.Setup(p => p.Collection).Returns([]);
        var adapter = new CollectionDataSourceAdapter<int>(mockProvider.Object);

        Assert.False(adapter is IAsyncDisposable);
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void Adapter_WorksWithStringType()
    {
        var mockProvider = new Mock<ICollectionProvider<string, IEnumerable<string>>>();
        mockProvider.Setup(p => p.Collection).Returns(["hello", "world"]);
        mockProvider.Setup(p => p.GetChangeToken()).Returns(NullChangeToken.Singleton);
        var adapter = new CollectionDataSourceAdapter<string>(mockProvider.Object);

        var collection = adapter.Collection;

        Assert.Equal(["hello", "world"], collection);
    }

    [Fact]
    public async Task Adapter_WorksWithComplexType()
    {
        var items = new[]
        {
            new TestItem(1, "one"),
            new TestItem(2, "two")
        };
        var dataSource = new StaticCollectionDataSource<TestItem>(items);
        await using var provider = new CollectionProvider<TestItem, IEnumerable<TestItem>>(
            i => i.ToList(),
            dataSource);
        var adapter = new CollectionDataSourceAdapter<TestItem>(provider);

        var collection = adapter.Collection.ToList();

        Assert.Equal(2, collection.Count);
        Assert.Equal(1, collection[0].Id);
        Assert.Equal("two", collection[1].Name);
    }

    private record TestItem(int Id, string Name);

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Adapter_WithRealProvider_DelegatesCorrectly()
    {
        var dataSource = new StaticCollectionDataSource<int>([1, 2, 3]);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            dataSource);
        var adapter = new CollectionDataSourceAdapter<int>(provider);

        var collection = adapter.Collection;
        var token = adapter.GetChangeToken();

        Assert.Equal([1, 2, 3], collection);
        Assert.NotNull(token);
        Assert.False(token.HasChanged);
    }

    [Fact]
    public async Task Adapter_WithRealProvider_ChangeTokenSignals_WhenProviderChanges()
    {
        var observableCollection = new ObservableCollection<int>([1, 2, 3]);
        using var observableDataSource = new ObservableCollectionDataSource<int>(observableCollection);
        await using var provider = new CollectionProvider<int, IEnumerable<int>>(
            items => items.ToList(),
            observableDataSource,
            owns: false);
        var adapter = new CollectionDataSourceAdapter<int>(provider);

        var token = adapter.GetChangeToken();
        var tcs = new TaskCompletionSource();
        token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

        observableCollection.Add(4);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(token.HasChanged);
    }

    #endregion
}
