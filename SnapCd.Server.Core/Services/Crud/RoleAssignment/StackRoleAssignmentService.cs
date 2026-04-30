using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.RoleAssignment;

public class StackRoleAssignmentServiceFactory(
    StackRoleAssignmentSecuredRepositoryFactory stackRoleAssignmentSecuredRepositoryFactory,
    UserStackRoleAssignmentSecuredRepositoryFactory userStackSecuredRepositoryFactory,
    ServicePrincipalStackRoleAssignmentSecuredRepositoryFactory servicePrincipalStackSecuredRepositoryFactory,
    GroupStackRoleAssignmentSecuredRepositoryFactory groupStackSecuredRepositoryFactory)
{
    public StackRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = stackRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userStackSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalStackSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupStackSecuredRepositoryFactory.Create(principalProvider);

        return new StackRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class StackRoleAssignmentService : IDisposable
{
    protected readonly StackRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserStackRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalStackRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupStackRoleAssignmentSecuredRepository GroupSecuredRepository;

    public StackRoleAssignmentService(
        StackRoleAssignmentSecuredRepository baseSecuredRepository,
        UserStackRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalStackRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupStackRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual StackRoleAssignment MapToEntity(StackRoleAssignmentDto dto, Guid organizationId)
    {
        return StackRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual StackRoleAssignmentDto MapToDto(StackRoleAssignment entity)
    {
        return StackRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(StackRoleAssignment entity, StackRoleAssignmentUpdateDto dto)
    {
        StackRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<StackRoleAssignmentDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<StackRoleAssignmentDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<StackRoleAssignmentDto> Create(StackRoleAssignmentDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserStackRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalStackRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupStackRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<StackRoleAssignmentDto> Update(StackRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserStackRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalStackRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupStackRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}