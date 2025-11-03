using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Repositories;

namespace ForbiddenPsalmBuilder.Core.Tests.Repositories;

public class InMemoryWarbandRepository : IWarbandRepository
{
    private readonly Dictionary<string, Warband> _warbands = new();

    public Task<IEnumerable<Warband>> GetAllAsync()
    {
        return Task.FromResult(_warbands.Values.AsEnumerable());
    }

    public Task<Warband?> GetByIdAsync(string id)
    {
        _warbands.TryGetValue(id, out var warband);
        return Task.FromResult(warband);
    }

    public Task<Warband> SaveAsync(Warband warband)
    {
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

            warband = newWarband;
        }
        else
        {
            warband.UpdateLastModified();
        }

        _warbands[warband.Id] = warband;
        return Task.FromResult(warband);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _warbands.Remove(id);
        return Task.FromResult(removed);
    }

    public Task<bool> ExistsAsync(string id)
    {
        var exists = _warbands.ContainsKey(id);
        return Task.FromResult(exists);
    }
}