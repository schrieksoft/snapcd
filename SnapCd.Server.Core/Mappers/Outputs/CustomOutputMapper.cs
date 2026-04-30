using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Mappers.Outputs;

public class CustomOutputMapper
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IVaultFactory _vaultFactory;
    private readonly IPrincipalProvider _principalProvider;
    private readonly SecretStoreSettings _secretStoreSettings;

    public CustomOutputMapper(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IVaultFactory vaultFactory,
        IOptions<SecretStoreSettings> secretStoreSettings,
        IPrincipalProvider principalProvider
    )
    {
        _dbContextFactory = dbContextFactory;
        _vaultFactory = vaultFactory;
        _principalProvider = principalProvider;
        _secretStoreSettings = secretStoreSettings.Value;
    }

    public (OutputSet outputSet, string outputKeyVaultUrl) MapOutputSet(OutputSetCreateDto outputSetDto, Guid moduleId, Guid organizationId)
    {
        var outputSet = new OutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            Timestamp = outputSetDto.Timestamp,
            Checksum = outputSetDto.Checksum,
            OrganizationId = organizationId,
            Outputs = []
        };

        var organization = GetOrganization(organizationId);
        var outputKeyVaultUrl = organization.OutputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultOutputKeyVaultUrl;

        if (outputSetDto.Outputs != null)
            foreach (var outputValue in outputSetDto.Outputs)
                if (outputValue.Sensitive == false)
                {
                    outputSet.Outputs.Add(new LiteralOutput
                    {
                        Id = Guid.NewGuid(),
                        Name = outputValue.Name,
                        Value = outputValue.Value,
                        OutputSetId = outputSet.Id,
                        OrganizationId = organizationId,
                        Type = outputValue.Type,
                        FromExtraFile = outputValue.FromExtraFile
                    });
                }
                else
                {
                    var secretOutput = CreateSecretOutput(outputValue, outputSet.Id, moduleId, organizationId);
                    secretOutput.FromExtraFile = outputValue.FromExtraFile;
                    outputSet.Outputs.Add(secretOutput);
                }

        return (outputSet, outputKeyVaultUrl);
    }


    public async Task<List<OutputReadDto>> MapOutputs(List<Output> outputs, Guid organizationId)
    {
        var organization = GetOrganization(organizationId);

        var keyVaultUrl = organization.OutputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultOutputKeyVaultUrl;

        var outputDtos = new ConcurrentBag<OutputReadDto>();

        await Parallel.ForEachAsync(outputs, async (output, _) =>
        {
            var outputDto = await MapOutput(output, keyVaultUrl);
            outputDtos.Add(outputDto);
        });

        return outputDtos.ToList();
    }


    public async Task<OutputReadDto> MapOutput(Output output, Guid organizationId)
    {
        // Only use this for single output calls since it must first make a db call to get the organization!
        var organization = GetOrganization(organizationId);
        var keyVaultUrl = organization.OutputKeyVaultUrl ?? _secretStoreSettings.AzureKeyVault.DefaultOutputKeyVaultUrl;
        return await MapOutput(output, keyVaultUrl);
    }

    public async Task<OutputReadDto> MapOutput(Output output, string keyVaultUrl)
    {
        var outputDto = new OutputReadDto
        {
            Id = output.Id,
            Name = output.Name,
            Type = output.Type,
            OutputSetId = output.OutputSetId,
            FromExtraFile = output.FromExtraFile
        };
        switch (output)
        {
            case LiteralOutput literalOutput:
            {
                outputDto.Value = literalOutput.Value;
                outputDto.Sensitive = false;
                return outputDto;
            }

            case SecretOutput secretOutput:
            {
                using var vault = _vaultFactory.Create(keyVaultUrl);
                var value = await vault.GetSecretAsync(secretOutput.RemoteSecretName);
                outputDto.Value = value;
                outputDto.Sensitive = true;
                return outputDto;
            }

            default:
                throw new NotImplementedException($"Unknown output type: {output.GetType().Name}");
        }
    }


    private string CreateRemoteSecretName(Guid organizationId, Guid moduleId, string outputName)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(outputName));
        var hash32 = BitConverter.ToString(hashBytes).Replace("-", "").ToLower()[..32];
        return $"output--{organizationId}--{moduleId}--{hash32}";
    }


    private SecretOutput CreateSecretOutput(OutputCreateDto outputCreateDto, Guid outputSetId, Guid moduleId, Guid organizationId)
    {
        return new SecretOutput
        {
            Id = Guid.NewGuid(),
            Name = outputCreateDto.Name,
            RemoteSecretName = CreateRemoteSecretName(organizationId, moduleId, outputCreateDto.Name),
            OrganizationId = organizationId,
            OutputSetId = outputSetId,
            Type = outputCreateDto.Type
        };
    }


    public Organization GetOrganization(Guid organizationId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Organizations.Single(x => x.Id == organizationId);
    }
}