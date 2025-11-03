namespace ForbiddenPsalmBuilder.Core.Services.State;

/// <summary>
/// Interface for state persistence (localStorage, sessionStorage, etc.)
/// </summary>
public interface IStateStorageService
{
    /// <summary>
    /// Save state data with given key
    /// </summary>
    Task SetItemAsync<T>(string key, T value);

    /// <summary>
    /// Load state data by key
    /// </summary>
    Task<T?> GetItemAsync<T>(string key);

    /// <summary>
    /// Remove state data by key
    /// </summary>
    Task RemoveItemAsync(string key);

    /// <summary>
    /// Clear all state data
    /// </summary>
    Task ClearAsync();
}
