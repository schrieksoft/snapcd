using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Services.ParamResolver.Helpers;

namespace SnapCd.Server.Core.Factories;

public class OutputSetParamResolverFactory
{
    private readonly OutputRepositoryFactory _repositoryFactory;
    private readonly CustomOutputMapper _outputMapper;
    private readonly SnapCdDbContext _dbContext;

    public OutputSetParamResolverFactory(
        OutputRepositoryFactory repositoryFactory,
        CustomOutputMapper outputMapper,
        SnapCdDbContext dbContext)
    {
        _repositoryFactory = repositoryFactory;
        _outputMapper = outputMapper;
        _dbContext = dbContext;
    }

    public virtual OutputSetParamResolver Create()
    {
        return new OutputSetParamResolver(
            _repositoryFactory.Create(),
            _outputMapper,
            _dbContext);
    }
}
