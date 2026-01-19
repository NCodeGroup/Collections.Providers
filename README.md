[![ci](https://github.com/NCodeGroup/Collections.Providers/actions/workflows/main.yml/badge.svg)](https://github.com/NCodeGroup/Collections.Providers/actions)
[![Nuget](https://img.shields.io/nuget/v/NCode.Collections.Providers.svg)](https://www.nuget.org/packages/NCode.Collections.Providers/)

# NCode.Collections.Providers

A .NET library for building dynamic, change-aware collection providers that aggregate multiple data sources with automatic change notifications.

## Features

### Core Abstractions (`NCode.Collections.Providers.Abstractions`)

- **`ICollectionDataSource<T>`** - Interface for data sources that provide a collection of items with change token support
- **`ICollectionProvider<TItem, TCollection>`** - Interface for providers that aggregate multiple data sources into a unified collection
- **`ISupportChangeToken`** - Interface for objects that support change notification via `IChangeToken`
- **`INullChangeToken`** - Marker interface for change tokens that never signal changes

### Data Source Implementations (`NCode.Collections.Providers`)

- **Static Data Source** - Wraps an immutable collection that never changes
- **Observable Data Source** - Wraps an `ObservableCollection<T>` with automatic change notifications when items are added, removed, or modified
- **Composite Data Source** - Aggregates multiple data sources into a single unified collection with consolidated change notifications
- **Periodic Polling Data Source** - Periodically fetches data from external sources (APIs, databases, files) with configurable refresh intervals and error handling

### Factory Interfaces

- **`ICollectionDataSourceFactory`** - Creates data source instances for different scenarios
- **`ICollectionProviderFactory`** - Creates collection provider instances that aggregate data sources

### Change Token Support

- Automatic propagation of change notifications from data sources to consumers
- Lazy initialization of change tokens for efficiency
- Thread-safe collection updates when changes are detected
- Support for additional change token producers beyond data sources (e.g., configuration changes)

### Dependency Injection Integration

- `AddCollectionProviders()` extension method for `IServiceCollection`
- Singleton registration of factories for optimal performance
- Support for DI-managed data source lifetimes

## Installation

```shell
dotnet add package NCode.Collections.Providers
```

Or for abstractions only:

```shell
dotnet add package NCode.Collections.Providers.Abstractions
```

## Quick Start

### Register Services

```csharp
services.AddCollectionProviders();
```

### Create a Static Data Source

```csharp
var factory = serviceProvider.GetRequiredService<ICollectionDataSourceFactory>();
var dataSource = factory.CreateStatic(new[] { "item1", "item2", "item3" });
```

### Create an Observable Data Source

```csharp
var observableCollection = new ObservableCollection<string>();
var dataSource = factory.CreateObservable(observableCollection);

// Changes to observableCollection will trigger change notifications
observableCollection.Add("new item");
```

### Create a Periodic Polling Data Source

```csharp
var dataSource = factory.CreatePeriodicPolling(
    state: httpClient,
    initialCollection: Array.Empty<User>(),
    refreshInterval: TimeSpan.FromMinutes(5),
    refreshCollectionAsync: async (client, current, ct) =>
    {
        var users = await client.GetFromJsonAsync<List<User>>("/api/users", ct);
        return RefreshCollectionResultFactory.Changed(users);
    },
    handleExceptionAsync: async (ex, ct) =>
    {
        logger.LogError(ex.SourceException, "Failed to refresh users");
    }
);
```

### Create a Collection Provider

```csharp
var providerFactory = serviceProvider.GetRequiredService<ICollectionProviderFactory>();
var provider = providerFactory.Create(
    collectionFactory: items => items.ToList(),
    dataSources: serviceProvider.GetServices<ICollectionDataSource<User>>()
);

// Access the aggregated collection
var users = provider.Collection;

// Subscribe to changes
ChangeToken.OnChange(provider.GetChangeToken, () =>
{
    Console.WriteLine("Collection changed!");
    var updatedUsers = provider.Collection;
});
```

## License

Licensed under the Apache License, Version 2.0. See [LICENSE.txt](LICENSE.txt) for details.

## Target Frameworks

- .NET 8.0
- .NET 10.0

## Release Notes
* v1.0.0 - Initial release
