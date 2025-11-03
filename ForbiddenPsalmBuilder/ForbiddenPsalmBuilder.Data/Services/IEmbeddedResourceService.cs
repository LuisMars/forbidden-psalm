using System.Text.Json;

namespace ForbiddenPsalmBuilder.Data.Services;

public interface IEmbeddedResourceService
{
    Task<string> GetResourceAsStringAsync(string resourcePath);
    Task<T?> GetResourceAsJsonAsync<T>(string resourcePath);
    Task<T?> GetGameResourceAsync<T>(string gameVariant, string fileName);
    Task<T?> GetSharedResourceAsync<T>(string fileName);
    IEnumerable<string> GetAllResourceNames();
    bool ResourceExists(string resourcePath);
}