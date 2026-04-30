using SnapCd.Contracts;

namespace SnapCd.Runner.Services.ModuleSourceRefresher;

public interface IModuleSourceRefresherFactory
{
    IModuleSourceRefresher Create(SourceType sourceType);
}

public class ModuleSourceRefresherFactory : IModuleSourceRefresherFactory
{
    public IModuleSourceRefresher Create(SourceType sourceType)
    {
        switch (sourceType)
        {
            case SourceType.Git:
                return new GitModuleSourceResolver();
            default:
                throw new NotImplementedException($"Source refresher for module of type \"{sourceType.ToString()}\" is not implemented.");
        }
    }
}