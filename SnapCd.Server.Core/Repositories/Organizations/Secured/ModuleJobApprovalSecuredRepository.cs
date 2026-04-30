using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleJobApprovalSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleJobApprovalRepositorySettings> approvalOptions,
    IOptions<ModuleJobRepositorySettings> jobOptions)
{
    public ModuleJobApprovalSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());

        // Create a single shared DbContext
        var dbContext = dbFactory.CreateDbContext();

        // Create both repositories with the shared DbContext
        var approvalRepository = new ModuleJobApprovalRepository(dbContext, principalProvider, bus, approvalOptions);
        var jobRepository = new ModuleJobRepository(dbContext, principalProvider, bus, jobOptions);
        var jobSecuredRepository = new ModuleJobSecuredRepository(jobRepository, principalProvider);

        return new ModuleJobApprovalSecuredRepository(
            approvalRepository,
            principalProvider,
            jobSecuredRepository);
    }
}

public class ModuleJobApprovalSecuredRepository : GenericSecuredRepository<
    ModuleJobApproval,
    ModuleJobApprovalReadDto,
    ModuleJobApprovalRepository,
    ModuleJobApprovalCreatedEvent,
    ModuleJobApprovalUpdatedEvent,
    ModuleJobApprovalDeletedEvent,
    ModuleJobApprovalRepositorySettings>
{
    private readonly ModuleJobSecuredRepository _moduleJobSecuredRepository;

    public ModuleJobApprovalSecuredRepository(
        ModuleJobApprovalRepository repository,
        IPrincipalProvider principalProvider,
        ModuleJobSecuredRepository moduleJobSecuredRepository)
        : base(repository, principalProvider)
    {
        _moduleJobSecuredRepository = moduleJobSecuredRepository;
    }

    public override void Dispose()
    {
        _moduleJobSecuredRepository?.Dispose();
        base.Dispose();
    }

    public async Task<List<ModuleJobApproval>> ListByJob(Guid moduleJobId, Guid organizationId)
    {
        // First check if user has permission to read the job
        var job = await _moduleJobSecuredRepository.Get(moduleJobId, organizationId);

        // Then return all approvals for that job
        return await Repository.ListByJob(moduleJobId, organizationId);
    }

    public override IQueryable<ModuleJobApproval> CreateQuery(Guid organizationId)
    {
        // Users can create approvals on modules where they have Contributor+ permission
        return ApprovalQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor],
            [StackRole.Owner, StackRole.Contributor],
            [NamespaceRole.Owner, NamespaceRole.Contributor],
            [ModuleRole.Owner, ModuleRole.Contributor]);
    }

    public override IQueryable<ModuleJobApproval> ReadQuery(Guid organizationId)
    {
        // Users can read approvals on modules where they have Reader+ permission
        return ApprovalQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
            [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
            [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
            [ModuleRole.Owner, ModuleRole.Contributor, ModuleRole.Reader]);
    }

    public override IQueryable<ModuleJobApproval> UpdateQuery(Guid organizationId)
    {
        // Users can update approvals on modules where they have Contributor+ permission
        return ApprovalQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor],
            [StackRole.Owner, StackRole.Contributor],
            [NamespaceRole.Owner, NamespaceRole.Contributor],
            [ModuleRole.Owner, ModuleRole.Contributor]);
    }

    public override IQueryable<ModuleJobApproval> DeleteQuery(Guid organizationId)
    {
        // Users can delete approvals on modules where they have Contributor+ permission
        return ApprovalQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor],
            [StackRole.Owner, StackRole.Contributor],
            [NamespaceRole.Owner, NamespaceRole.Contributor],
            [ModuleRole.Owner, ModuleRole.Contributor]);
    }

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        // parentId is the ModuleJobId
        var job = Repository.DbContext.ModuleJobs
            .FirstOrDefault(j => j.Id == parentId && j.OrganizationId == organizationId);

        if (job == null)
            return false;

        // Check role assignments against the module hierarchy directly,
        // rather than querying existing ModuleJobApproval records (which may not exist yet).
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateApprovalInModule<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserModuleRoleAssignment>(
                organizationId, principalId, job.ModuleId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateApprovalInModule<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalModuleRoleAssignment>(
                organizationId, principalId, job.ModuleId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private bool CanCreateApprovalInModule<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid moduleId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        var hasModuleRole = Repository.DbContext.Set<TModuleRoleAssignment>()
            .Any(ra => ra.ModuleId == moduleId
                        && ra.OrganizationId == organizationId
                        && ra.PrincipalId == principalId
                        && (ra.RoleName == ModuleRole.Owner || ra.RoleName == ModuleRole.Contributor));
        if (hasModuleRole) return true;

        var hasNamespaceRole = (
            from module in Repository.DbContext.Modules
            where module.Id == moduleId && module.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && (assignment.RoleName == NamespaceRole.Owner || assignment.RoleName == NamespaceRole.Contributor)
            select assignment
        ).Any();
        if (hasNamespaceRole) return true;

        var hasStackRole = (
            from module in Repository.DbContext.Modules
            where module.Id == moduleId && module.OrganizationId == organizationId
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && (assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor)
            select assignment
        ).Any();
        if (hasStackRole) return true;

        var hasOrgRole = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                        && ra.PrincipalId == principalId
                        && (ra.RoleName == OrganizationRole.Owner || ra.RoleName == OrganizationRole.Contributor));

        return hasOrgRole;
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        // Additional check: user can only update their own approval
        var approval = Repository.DbContext.Set<ModuleJobApproval>()
            .FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId);

        if (approval == null)
            return false;

        var principalId = PrincipalProvider.GetSubject(organizationId);
        if (approval.PrincipalId != principalId)
            return false;

        return UpdateQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        // Additional check: user can only delete their own approval
        var approval = Repository.DbContext.Set<ModuleJobApproval>()
            .FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId);

        if (approval == null)
            return false;

        var principalId = PrincipalProvider.GetSubject(organizationId);
        if (approval.PrincipalId != principalId)
            return false;

        return DeleteQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override string GetParentEntityName()
    {
        return "ModuleJob";
    }


    public override async Task<ModuleJobApproval> Create(ModuleJobApproval entity, bool inTransaction = true)
    {
        // Ensure the approval is for the calling principal
        var principalId = PrincipalProvider.GetSubject(entity.OrganizationId);
        if (entity.PrincipalId != principalId)
            throw new PrincipalNotAuthorizedException(
                $"Attempting to create a ModuleJobApproval on behalf of principal with ID {entity.PrincipalId} using principal with ID {principalId}. A principal can only create ModuleJobApproval on its own behalf.");

        return await base.Create(entity);
    }

    public override async Task<ModuleJobApproval> Update(ModuleJobApproval entity, bool inTransaction = true)
    {
        // Ensure the approval is for the calling principal
        var principalId = PrincipalProvider.GetSubject(entity.OrganizationId);
        if (entity.PrincipalId != principalId)
            throw new PrincipalNotAuthorizedException(
                $"Attempting to update a ModuleJobApproval on behalf of principal with ID {entity.PrincipalId} using principal with ID {principalId}. A principal can only update ModuleJobApproval on its own behalf.");

        return await base.Update(entity);
    }

    #region Permission Query Methods

    protected IQueryable<ModuleJobApproval> ApprovalQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => ApprovalRoleQuery<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserModuleRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles, moduleRoles),
            PrincipalDiscriminator.ServicePrincipal => ApprovalRoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalModuleRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles, moduleRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    protected IQueryable<ModuleJobApproval> ApprovalRoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TModuleRoleAssignment : class, IModuleRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Role assignment on Module (through ModuleJob)
        var approvalsFromModuleRoles =
            from approval in Repository.DbContext.Set<ModuleJobApproval>()
            join job in Repository.DbContext.ModuleJobs
                on new { ModuleJobId = approval.ModuleJobId, approval.OrganizationId } equals new { ModuleJobId = job.Id, job.OrganizationId }
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = job.ModuleId, job.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where approval.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select approval;

        // Role assignment on Namespace (through ModuleJob -> Module)
        var approvalsFromNamespaceRoles =
            from approval in Repository.DbContext.Set<ModuleJobApproval>()
            join job in Repository.DbContext.ModuleJobs
                on new { ModuleJobId = approval.ModuleJobId, approval.OrganizationId } equals new { ModuleJobId = job.Id, job.OrganizationId }
            join module in Repository.DbContext.Modules
                on new { ModuleId = job.ModuleId, job.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where approval.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select approval;

        // Role assignment on Stack (through ModuleJob -> Module -> Namespace)
        var approvalsFromStackRoles =
            from approval in Repository.DbContext.Set<ModuleJobApproval>()
            join job in Repository.DbContext.ModuleJobs
                on new { ModuleJobId = approval.ModuleJobId, approval.OrganizationId } equals new { ModuleJobId = job.Id, job.OrganizationId }
            join module in Repository.DbContext.Modules
                on new { ModuleId = job.ModuleId, job.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where approval.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select approval;

        // Role assignment on Organization
        var approvalsFromOrganizationRoles =
            from approval in Repository.DbContext.Set<ModuleJobApproval>()
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on approval.OrganizationId equals assignment.OrganizationId
            where approval.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select approval;

        // Group-based role assignments (not implemented yet - approvals are personal)
        // ModuleJobApproval is a personal action, so group-based permissions are not typically used here
        // But we implement it for completeness in case needed in the future

        return approvalsFromModuleRoles
            .Concat(approvalsFromNamespaceRoles)
            .Concat(approvalsFromStackRoles)
            .Concat(approvalsFromOrganizationRoles);
    }

    #endregion
}