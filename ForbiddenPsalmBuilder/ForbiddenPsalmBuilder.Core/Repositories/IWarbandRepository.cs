using ForbiddenPsalmBuilder.Core.Models.Warband;

namespace ForbiddenPsalmBuilder.Core.Repositories;

public interface IWarbandRepository
{
    Task<IEnumerable<Warband>> GetAllAsync();
    Task<Warband?> GetByIdAsync(string id);
    Task<Warband> SaveAsync(Warband warband);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}