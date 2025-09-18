using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Reliable.HttpClient.Caching.Providers;

using Xunit;

namespace Reliable.HttpClient.Caching.Tests.Providers;

public class MemoryCacheProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheProvider<TestResponse>> _logger;
    private readonly MemoryCacheProvider<TestResponse> _provider;

    public MemoryCacheProviderTests()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        _logger = _serviceProvider.GetRequiredService<ILogger<MemoryCacheProvider<TestResponse>>>();

        _provider = new MemoryCacheProvider<TestResponse>(_memoryCache, _logger);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        TestResponse? result = await _provider.GetAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_AndGetAsync_ReturnsStoredValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestResponse { Id = 1, Name = "Test" };
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await _provider.SetAsync(key, value, expiry, CancellationToken.None);
        TestResponse? result = await _provider.GetAsync(key, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task SetAsync_WithZeroExpiry_DoesNotStoreValue()
    {
        // Arrange
        var value = new TestResponse { Id = 1, Name = "Test" };
        TimeSpan expiry = TimeSpan.Zero;

        // Act
        await _provider.SetAsync("test-key", value, expiry, CancellationToken.None);
        TestResponse? result = await _provider.GetAsync("test-key", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithNegativeExpiry_DoesNotStoreValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestResponse { Id = 1, Name = "Test" };
        var expiry = TimeSpan.FromMinutes(-1);

        // Act
        await _provider.SetAsync(key, value, expiry, CancellationToken.None);
        TestResponse? result = await _provider.GetAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestResponse { Id = 1, Name = "Test" };
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await _provider.SetAsync(key, value, expiry, CancellationToken.None);
        TestResponse? beforeRemove = await _provider.GetAsync(key, CancellationToken.None);

        await _provider.RemoveAsync(key, CancellationToken.None);
        TestResponse? afterRemove = await _provider.GetAsync(key, CancellationToken.None);

        // Assert
        beforeRemove.Should().NotBeNull();
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var key = "non-existent-key";

        // Act & Assert
        Func<Task> action = () => _provider.RemoveAsync(key, CancellationToken.None);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearAsync_RemovesAllCachedItems()
    {
        // Arrange
        var key1 = "test-key-1";
        var key2 = "test-key-2";
        var value1 = new TestResponse { Id = 1, Name = "Test1" };
        var value2 = new TestResponse { Id = 2, Name = "Test2" };
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await _provider.SetAsync(key1, value1, expiry, CancellationToken.None);
        await _provider.SetAsync(key2, value2, expiry, CancellationToken.None);

        TestResponse? beforeClear1 = await _provider.GetAsync(key1, CancellationToken.None);
        TestResponse? beforeClear2 = await _provider.GetAsync(key2, CancellationToken.None);

        await _provider.ClearAsync(CancellationToken.None);

        TestResponse? afterClear1 = await _provider.GetAsync(key1, CancellationToken.None);
        TestResponse? afterClear2 = await _provider.GetAsync(key2, CancellationToken.None);

        // Assert
        beforeClear1.Should().NotBeNull();
        beforeClear2.Should().NotBeNull();
        afterClear1.Should().BeNull();
        afterClear2.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestResponse { Id = 1, Name = "Test" };
        var expiry = TimeSpan.FromMinutes(5);

        // Act & Assert
        Func<Task> action = () => _provider.SetAsync(null!, value, expiry, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestResponse { Id = 1, Name = "Test" };
        var expiry = TimeSpan.FromMinutes(5);

        // Act & Assert
        Func<Task> action = () => _provider.SetAsync("", value, expiry, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        Func<Task> action = () => _provider.GetAsync(null!, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        Func<Task> action = () => _provider.RemoveAsync(null!, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithNullValue_DoesNotStore()
    {
        // Arrange
        var key = "test-key";
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await _provider.SetAsync(key, null!, expiry, CancellationToken.None);
        TestResponse? result = await _provider.GetAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        var key = "test-key";
        var value1 = new TestResponse { Id = 1, Name = "Test1" };
        var value2 = new TestResponse { Id = 2, Name = "Test2" };
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await _provider.SetAsync(key, value1, expiry, CancellationToken.None);
        TestResponse? first = await _provider.GetAsync(key, CancellationToken.None);

        await _provider.SetAsync(key, value2, expiry, CancellationToken.None);
        TestResponse? second = await _provider.GetAsync(key, CancellationToken.None);

        // Assert
        first!.Id.Should().Be(1);
        first.Name.Should().Be("Test1");

        second!.Id.Should().Be(2);
        second.Name.Should().Be("Test2");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    public class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
