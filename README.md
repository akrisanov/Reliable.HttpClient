# Reliable.HttpClient

## Core Package

[![NuGet Version](https://img.shields.io/nuget/v/Reliable.HttpClient)](https://www.nuget.org/packages/Reliable.HttpClient/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Reliable.HttpClient)](https://www.nuget.org/packages/Reliable.HttpClient/)

## Caching Extension

[![NuGet Version](https://img.shields.io/nuget/v/Reliable.HttpClient.Caching)](https://www.nuget.org/packages/Reliable.HttpClient.Caching/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Reliable.HttpClient.Caching)](https://www.nuget.org/packages/Reliable.HttpClient.Caching/)

## Project Status

[![Build Status](https://github.com/akrisanov/Reliable.HttpClient/workflows/Build%20%26%20Test/badge.svg)](https://github.com/akrisanov/Reliable.HttpClient/actions)
[![codecov](https://codecov.io/gh/akrisanov/Reliable.HttpClient/branch/main/graph/badge.svg)](https://codecov.io/gh/akrisanov/Reliable.HttpClient)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/akrisanov/Reliable.HttpClient)](LICENSE)

A comprehensive resilience and caching ecosystem for HttpClient with built-in retry policies, circuit breakers, and intelligent response caching.
Based on [Polly](https://github.com/App-vNext/Polly) but with zero configuration required.

## 🎯 Choose Your Approach

**Not sure which approach to use?** [→ Read our Choosing Guide](docs/choosing-approach.md)

| Your Use Case | Recommended Approach | Documentation |
|---------------|---------------------|---------------|
| **Single API with 1-2 entity types** | Traditional Generic | [Getting Started](docs/getting-started.md) |
| **REST API with 5+ entity types** | Universal Handlers | [Common Scenarios - Universal REST API](docs/examples/common-scenarios.md#universal-rest-api-client) |
| **Custom serialization/error handling** | Custom Response Handler | [Advanced Usage](docs/advanced-usage.md) |

## Packages

| Package                           | Purpose                                  | Version                          |
|-----------------------------------|------------------------------------------|----------------------------------|
| **Reliable.HttpClient**           | Core resilience (retry + circuit breaker) | `dotnet add package Reliable.HttpClient` |
| **Reliable.HttpClient.Caching**   | HTTP response caching extension          | `dotnet add package Reliable.HttpClient.Caching` |

## Why Choose This Ecosystem?

- **Zero Configuration**: Works out of the box with sensible defaults
- **Complete Solution**: Resilience + Caching in one ecosystem
- **Lightweight**: Minimal overhead, maximum reliability
- **Production Ready**: Used by companies in production environments
- **Easy Integration**: One line of code to add resilience, two lines for caching
- **Secure**: SHA256-based cache keys prevent collisions and attacks
- **Flexible**: Use core resilience alone or add caching as needed

## Quick Start

```bash
dotnet add package Reliable.HttpClient
```

```csharp
// Add to your Program.cs
builder.Services.AddHttpClient<ApiClient>(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddResilience(); // That's it! ✨

// Use anywhere
public class ApiClient(HttpClient client)
{
    public async Task<Data> GetDataAsync() =>
        await client.GetFromJsonAsync<Data>("/endpoint");
}
```

**You now have:** Automatic retries + Circuit breaker + Smart error handling

> 🚀 **Need details?** See [Getting Started Guide](docs/getting-started.md) for step-by-step setup
> 🆕 **Building REST APIs?** Check [Universal Response Handlers](docs/examples/common-scenarios.md#universal-rest-api-client)

## Key Features

✅ **Zero Configuration** - Works out of the box
✅ **Resilience** - Retry + Circuit breaker
✅ **Caching** - Intelligent HTTP response caching
✅ **Production Ready** - Used by companies in production

> 📖 **Full Feature List**: [Documentation](docs/README.md#key-features)

## Need Customization?

```csharp
// Custom settings
.AddResilience(options => options.Retry.MaxRetries = 5);

// Ready-made presets
.AddResilience(HttpClientPresets.SlowExternalApi());
```

> 📖 **Full Configuration**: [Configuration Guide](docs/configuration.md)

## Trusted By

Organizations using Reliable.HttpClient in production:

[![PlanFact](https://raw.githubusercontent.com/akrisanov/Reliable.HttpClient/refs/heads/main/docs/assets/logos/planfact.png)](https://planfact.io)

## Documentation

- [Getting Started Guide](docs/getting-started.md) - Step-by-step setup
- [Common Scenarios](docs/examples/common-scenarios.md) - Real-world examples 🆕
- [Configuration Reference](docs/configuration.md) - Complete options
- [Advanced Usage](docs/advanced-usage.md) - Advanced patterns
- [HTTP Caching Guide](docs/caching.md) - Caching documentation

## Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
