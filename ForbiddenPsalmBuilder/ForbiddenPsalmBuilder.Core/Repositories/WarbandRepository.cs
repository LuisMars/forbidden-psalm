using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Services.State;

namespace ForbiddenPsalmBuilder.Core.Repositories;

public class WarbandRepository : IWarbandRepository
{
    private readonly IStateStorageService _storageService;
    private const string WARBANDS_KEY = "warbands";

    public WarbandRepository(IStateStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<IEnumerable<Warband>> GetAllAsync()
    {
        var warbandsData = await _storageService.GetItemAsync<Dictionary<string, Warband>>(WARBANDS_KEY);
        return warbandsData?.Values ?? Enumerable.Empty<Warband>();
    }

    public async Task<Warband?> GetByIdAsync(string id)
    {
        var warbandsData = await _storageService.GetItemAsync<Dictionary<string, Warband>>(WARBANDS_KEY);
        if (warbandsData?.TryGetValue(id, out var warband) == true)
        {
            return warband;
        }
        return null;
    }

    public async Task<Warband> SaveAsync(Warband warband)
    {
        var warbandsData = await _storageService.GetItemAsync<Dictionary<string, Warband>>(WARBANDS_KEY)
            ?? new Dictionary<string, Warband>();

        if (string.IsNullOrEmpty(warband.Id))
        {
            var newWarband = new Warband(warband.Name, warband.GameVariant)
            {
                Id = Guid.NewGuid().ToString(),
                Gold = warband.Gold,
                Experience = warband.Experience,
                LastModified = DateTime.UtcNow
            };

            // Copy members
            foreach (var member in warband.Members)
            {
                newWarband.Members.Add(member);
            }

            // Copy upgrades
            foreach (var upgrade in warband.UpgradesPurchased)
            {
                newWarband.UpgradesPurchased.Add(upgrade);
            }

            warband = newWarband;
        }
        else
        {
            warband.UpdateLastModified();
        }

        warbandsData[warband.Id] = warband;
        await _storageService.SetItemAsync(WARBANDS_KEY, warbandsData);

        return warband;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var warbandsData = await _storageService.GetItemAsync<Dictionary<string, Warband>>(WARBANDS_KEY);
        if (warbandsData == null)
            return false;

        var removed = warbandsData.Remove(id);
        if (removed)
        {
            await _storageService.SetItemAsync(WARBANDS_KEY, warbandsData);
        }

        return removed;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        var warbandsData = await _storageService.GetItemAsync<Dictionary<string, Warband>>(WARBANDS_KEY);
        return warbandsData?.ContainsKey(id) ?? false;
    }
}