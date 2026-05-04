using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ServicePrincipalServiceFactory(ServicePrincipalSecuredRepositoryFactory securedRepositoryFactory)
{
    public ServicePrincipalService Create()
    {
        return new ServicePrincipalService(securedRepositoryFactory.Create());
    }
}

public class ServicePrincipalService : GenericCrudService<
    ServicePrincipal,
    ServicePrincipalCreateDto,
    ServicePrincipalUpdateDto,
    ServicePrincipalReadDto,
    ServicePrincipalSecuredRepository,
    ServicePrincipalRepository,
    ServicePrincipalCreatedEvent,
    ServicePrincipalUpdatedEvent,
    ServicePrincipalDeletedEvent,
    ServicePrincipalRepositorySettings>
{
    public ServicePrincipalService(
        ServicePrincipalSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ServicePrincipal MapToEntity(ServicePrincipalCreateDto dto, Guid organizationId)
    {
        return ServicePrincipalMapper.ToEntity(dto, organizationId);
    }

    protected override ServicePrincipalReadDto MapToDto(ServicePrincipal entity)
    {
        return ServicePrincipalMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ServicePrincipal entity, ServicePrincipalUpdateDto dto)
    {
        ServicePrincipalMapper.UpdateEntity(entity, dto);
    }

    public async Task<ServicePrincipalReadDto> GetByClientId(string clientId, Guid organizationId)
    {
        var entity = await SecuredRepository.GetByClientId(clientId, organizationId)
            ?? throw new KeyNotFoundException($"Service principal with client ID '{clientId}' not found in organization {organizationId}.");
        return MapToDto(entity);
    }

    public override async Task<ServicePrincipalReadDto> Create(ServicePrincipalCreateDto dto, Guid organizationId)
    {
        var secretNotHashed = dto.ClientSecret;
        dto.ClientSecret = SecretHashingHelper.ObfuscateClientSecret(dto.ClientSecret ?? string.Empty);

        var result = await base.Create(dto, organizationId);

        result.ClientSecret = secretNotHashed;

        return result;
    }

    public override async Task<ServicePrincipalReadDto> Update(ServicePrincipalUpdateDto dto, Guid id, Guid organizationId)
    {
        var secretNotHashed = dto.ClientSecret;

        if (string.IsNullOrEmpty(secretNotHashed))
        {
            var sp = await SecuredRepository.GetByClientId(dto.ClientId, organizationId)
                ?? throw new KeyNotFoundException($"Service principal with client ID '{dto.ClientId}' not found in organization {organizationId}.");
            dto.ClientSecret = sp.ClientSecret;
        }
        else
        {
            dto.ClientSecret = SecretHashingHelper.ObfuscateClientSecret(dto.ClientSecret ?? string.Empty);
        }

        var entity = await SecuredRepository.Get(id, organizationId);
        UpdateEntityFromDto(entity, dto);
        entity = await SecuredRepository.Update(entity);
        var returnDto = MapToDto(entity);

        returnDto.ClientSecret = secretNotHashed;

        return returnDto;
    }


    public async Task<ServicePrincipalReadDto> GetWithSecretVerify(Guid id, string secret, Guid organizationId)
    {
        var dto = await Get(id, organizationId);

        var clientSecretMatches = SecretHashingHelper.VerifyHashedSecret(dto.ClientSecret ?? string.Empty, secret);

        if (!clientSecretMatches)
            dto.ClientSecret = $"ClientSecret differs, ConcurrencyToken {Guid.NewGuid().ToString()}";
        else
            dto.ClientSecret = secret;

        return dto;
    }
}