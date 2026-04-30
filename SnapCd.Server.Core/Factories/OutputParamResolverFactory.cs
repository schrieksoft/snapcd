using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Services.ParamResolver.Helpers;

namespace SnapCd.Server.Core.Factories;

public class OutputParamResolverFactory
{
    private readonly OutputRepositoryFactory _repositoryFactory;
    private readonly CustomOutputMapper _outputMapper;
    private readonly SnapCdDbContext _dbContext;

    public OutputParamResolverFactory(
        OutputRepositoryFactory repositoryFactory,
        CustomOutputMapper outputMapper,
        SnapCdDbContext dbContext)
    {
        _repositoryFactory = repositoryFactory;
        _outputMapper = outputMapper;
        _dbContext = dbContext;
    }

    public virtual OutputParamResolver<ModuleParamFromOutput> CreateForParams()
    {
        return new OutputParamResolver<ModuleParamFromOutput>(
            _repositoryFactory.Create(),
            _outputMapper,
            _dbContext);
    }

    public virtual OutputParamResolver<ModuleEnvVarFromOutput> CreateForEnvVars()
    {
        return new OutputParamResolver<ModuleEnvVarFromOutput>(
            _repositoryFactory.Create(),
            _outputMapper,
            _dbContext);
    }
}
