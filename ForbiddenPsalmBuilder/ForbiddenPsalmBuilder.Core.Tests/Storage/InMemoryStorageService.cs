using ForbiddenPsalmBuilder.Core.Services.State;
using System.Text.Json;

namespace ForbiddenPsalmBuilder.Core.Tests.Storage;

/// <summary>
/// In-memory storage service for testing (implements IStateStorageService)
/// </summary>
public class InMemoryStorageService : IStateStorageService
{
    private readonly Dictionary<string, string> _storage = new();

    public Task<T?> GetItemAsync<T>(string key)
    {
        if (_storage.TryGetValue(key, out var json))
        {
            var item = JsonSerializer.Deserialize<T>(json);
            return Task.FromResult(item);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        _storage[key] = json;
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _storage.Clear();
        return Task.CompletedTask;
    }
}