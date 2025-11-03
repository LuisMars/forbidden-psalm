using ForbiddenPsalmBuilder.Core.Repositories;

namespace ForbiddenPsalmBuilder.Core.Tests.Repositories;

public class InMemoryWarbandRepositoryTests : WarbandRepositoryContractTests
{
    protected override IWarbandRepository CreateRepository()
    {
        return new InMemoryWarbandRepository();
    }
}