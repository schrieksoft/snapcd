// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
