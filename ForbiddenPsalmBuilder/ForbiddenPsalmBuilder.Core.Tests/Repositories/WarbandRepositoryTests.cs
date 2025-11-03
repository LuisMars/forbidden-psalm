using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Core.Tests.Storage;

namespace ForbiddenPsalmBuilder.Core.Tests.Repositories;

public class WarbandRepositoryTests : WarbandRepositoryContractTests
{
    protected override IWarbandRepository CreateRepository()
    {
        var storageService = new InMemoryStorageService();
        return new WarbandRepository(storageService);
    }
}