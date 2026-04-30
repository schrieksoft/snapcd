using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.RoleAssignment;

public class RunnerRoleAssignmentServiceFactory(
    RunnerRoleAssignmentSecuredRepositoryFactory runnerRoleAssignmentSecuredRepositoryFactory,
    UserRunnerRoleAssignmentSecuredRepositoryFactory userRunnerSecuredRepositoryFactory,
    ServicePrincipalRunnerRoleAssignmentSecuredRepositoryFactory servicePrincipalRunnerSecuredRepositoryFactory,
    GroupRunnerRoleAssignmentSecuredRepositoryFactory groupRunnerSecuredRepositoryFactory)
{
    public RunnerRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = runnerRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userRunnerSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalRunnerSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupRunnerSecuredRepositoryFactory.Create(principalProvider);

        return new RunnerRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class RunnerRoleAssignmentService : IDisposable
{
    protected readonly RunnerRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserRunnerRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalRunnerRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupRunnerRoleAssignmentSecuredRepository GroupSecuredRepository;

    public RunnerRoleAssignmentService(
        RunnerRoleAssignmentSecuredRepository baseSecuredRepository,
        UserRunnerRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalRunnerRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupRunnerRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual RunnerRoleAssignment MapToEntity(RunnerRoleAssignmentReadDto dto, Guid organizationId)
    {
        return RunnerRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual RunnerRoleAssignmentReadDto MapToDto(RunnerRoleAssignment entity)
    {
        return RunnerRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(RunnerRoleAssignment entity, RunnerRoleAssignmentUpdateDto dto)
    {
        RunnerRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<RunnerRoleAssignmentReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<RunnerRoleAssignmentReadDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<RunnerRoleAssignmentReadDto> Create(RunnerRoleAssignmentReadDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserRunnerRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalRunnerRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupRunnerRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<RunnerRoleAssignmentReadDto> Update(RunnerRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserRunnerRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalRunnerRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupRunnerRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}