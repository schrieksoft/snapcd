using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.RoleAssignment;

public class OrganizationRoleAssignmentServiceFactory(
    OrganizationRoleAssignmentSecuredRepositoryFactory organizationRoleAssignmentSecuredRepositoryFactory,
    UserOrganizationRoleAssignmentSecuredRepositoryFactory userOrganizationSecuredRepositoryFactory,
    ServicePrincipalOrganizationRoleAssignmentSecuredRepositoryFactory servicePrincipalOrganizationSecuredRepositoryFactory,
    GroupOrganizationRoleAssignmentSecuredRepositoryFactory groupOrganizationSecuredRepositoryFactory)
{
    public OrganizationRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = organizationRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userOrganizationSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalOrganizationSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupOrganizationSecuredRepositoryFactory.Create(principalProvider);

        return new OrganizationRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class OrganizationRoleAssignmentService : IDisposable
{
    protected readonly OrganizationRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserOrganizationRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalOrganizationRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupOrganizationRoleAssignmentSecuredRepository GroupSecuredRepository;

    public OrganizationRoleAssignmentService(
        OrganizationRoleAssignmentSecuredRepository baseSecuredRepository,
        UserOrganizationRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalOrganizationRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupOrganizationRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual OrganizationRoleAssignment MapToEntity(OrganizationRoleAssignmentReadDto dto, Guid organizationId)
    {
        return OrganizationRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual OrganizationRoleAssignmentReadDto MapToDto(OrganizationRoleAssignment entity)
    {
        return OrganizationRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(OrganizationRoleAssignment entity, OrganizationRoleAssignmentUpdateDto dto)
    {
        OrganizationRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<OrganizationRoleAssignmentReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<OrganizationRoleAssignmentReadDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<OrganizationRoleAssignmentReadDto> Create(OrganizationRoleAssignmentReadDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserOrganizationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalOrganizationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupOrganizationRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<OrganizationRoleAssignmentReadDto> Update(OrganizationRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserOrganizationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalOrganizationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupOrganizationRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}