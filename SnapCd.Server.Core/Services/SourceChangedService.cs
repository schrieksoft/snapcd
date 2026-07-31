// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services;

public class SourceChangedServiceFactory
{
    private readonly IBus _bus;
    private readonly IDbContextFactory<SnapCdDbContext> _dbFactory;
    private readonly SourceRefreshDispatcher _dispatcher;

    public SourceChangedServiceFactory(
        IBus bus,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        SourceRefreshDispatcher dispatcher
    )
    {
        _bus = bus;
        _dbFactory = dbFactory;
        _dispatcher = dispatcher;
    }

    public SourceChangedService Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = _dbFactory.CreateDbContext();

        return new SourceChangedService(_bus, dbContext, principalProvider, _dispatcher);
    }
}

public class SourceChangedService
{
    private readonly IBus _bus;
    private readonly SnapCdDbContext _dbContext;
    private readonly IPrincipalProvider _principalProvider;
    private readonly SourceRefreshDispatcher _dispatcher;

    public SourceChangedService(
        IBus bus,
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        SourceRefreshDispatcher dispatcher
    )
    {
        _bus = bus;
        _dbContext = dbContext;
        _principalProvider = principalProvider;
        _dispatcher = dispatcher;
    }

    public PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.SourceChangeNotifier]
    };

    public bool CanNotify(PrincipalDiscriminator principalDiscriminator, Guid principalId, List<OrganizationRole> validRoles, Guid organizationId)
    {
        return principalDiscriminator switch
        {
            PrincipalDiscriminator.User => _dbContext.UserOrganizationRoleAssignments
                .Any(x =>
                    x.OrganizationId == organizationId &&
                    x.PrincipalId == principalId &&
                    validRoles.Contains(x.RoleName)),
            PrincipalDiscriminator.ServicePrincipal => _dbContext.ServicePrincipalOrganizationRoleAssignments
                .Any(x =>
                    x.OrganizationId == organizationId &&
                    x.PrincipalId == principalId &&
                    validRoles.Contains(x.RoleName)),
            _ => false
        };
    }

    public async Task NotifyChange(SourceChangedDto dto, Guid organizationId)
    {
        var validRoles = CreatePermissionMap.OrganizationRoles;
        if (!CanNotify(_principalProvider.GetPrincipalDiscriminator(), _principalProvider.GetSubject(organizationId), validRoles, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{_principalProvider.GetPrincipalDiscriminator()} with ID {_principalProvider.GetSubject(organizationId)} does not have permission to notify source changes");

        var modules = _dbContext.Modules
            .Include(x => x.AdditionalTriggerPaths)
            .Include(x => x.Namespace).ThenInclude(n => n.AdditionalTriggerPaths)
            .Where(x =>
                x.SourceUrl == dto.SourceUrl &&
                x.SourceRevision == dto.SourceRevision &&
                x.SourceType == dto.SourceType &&
                x.TriggerOnSourceChangedNotification &&
                x.OrganizationId == organizationId
            )
            .ToList();

        // Modules without path-scoped triggering keep today's behaviour: the notification IS the trigger.
        foreach (var module in modules.Where(m => !TriggerPathClosure.FilterEnabled(m)))
            await _bus.Publish(new GatekeepingJobRequested
            {
                ModuleId = module.Id,
                OrganizationId = organizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false
            }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });

        // Filter-enabled modules converge on the closure-hash primitive: dispatch one targeted refresh per
        // refresh group and let SourceRefreshCompletedCompetingConsumer decide from the reported hashes.
        var groups = modules
            .Where(TriggerPathClosure.FilterEnabled)
            .GroupBy(x => new { x.SourceType, x.SourceUrl, x.SourceRevision, x.SourceRevisionType, x.OrganizationId, x.RunnerId });

        foreach (var group in groups)
            await _dispatcher.DispatchRefresh(
                group.Key.OrganizationId,
                group.Key.RunnerId,
                group.Key.SourceUrl,
                group.Key.SourceRevision,
                group.Key.SourceType,
                group.Key.SourceRevisionType,
                group
                    .SelectMany(TriggerPathClosure.WatchedPaths)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .ToList(),
                triggeredByNotification: true);
    }
}