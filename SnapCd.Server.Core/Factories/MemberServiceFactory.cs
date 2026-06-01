// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Email;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Factories;

public class MemberServiceFactory
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly OrganizationUserRepositoryFactory _orgUserRepoFactory;
    private readonly UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> _userManagerFactory;
    private readonly IQuotaUsageForInvitationServiceFactory _quotaServiceFactory;
    private readonly IOptions<InvitationSettings> _settings;
    private readonly ISnapCdEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBus _bus;
    private readonly ILogger<MemberService> _logger;

    public MemberServiceFactory(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        OrganizationUserRepositoryFactory orgUserRepoFactory,
        UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> userManagerFactory,
        IQuotaUsageForInvitationServiceFactory quotaServiceFactory,
        IOptions<InvitationSettings> settings,
        ISnapCdEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor,
        IBus bus,
        ILogger<MemberService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _orgUserRepoFactory = orgUserRepoFactory;
        _userManagerFactory = userManagerFactory;
        _quotaServiceFactory = quotaServiceFactory;
        _settings = settings;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
        _bus = bus;
        _logger = logger;
    }

    public MemberServiceScope Create()
    {
        var dbContext = _dbContextFactory.CreateDbContext();
        var orgUserRepo = _orgUserRepoFactory.Create();
        var userManagerScope = _userManagerFactory.Create();
        var quotaServiceScope = _quotaServiceFactory.Create();
        var memberService = new MemberService(dbContext, orgUserRepo, userManagerScope.UserManager, quotaServiceScope.Service, _settings, _emailSender, _httpContextAccessor, _bus, _logger);
        return new MemberServiceScope(memberService, dbContext, userManagerScope, quotaServiceScope);
    }
}

public class MemberServiceScope : IDisposable
{
    public MemberService Service { get; }
    private readonly SnapCdDbContext _dbContext;
    private readonly UserManagerScope<User> _userManagerScope;
    private readonly IQuotaUsageForInvitationServiceScope _quotaServiceScope;

    internal MemberServiceScope(MemberService service, SnapCdDbContext dbContext, UserManagerScope<User> userManagerScope, IQuotaUsageForInvitationServiceScope quotaServiceScope)
    {
        Service = service;
        _dbContext = dbContext;
        _userManagerScope = userManagerScope;
        _quotaServiceScope = quotaServiceScope;
    }

    public void Dispose()
    {
        Service?.Dispose();
        _dbContext?.Dispose();
        _userManagerScope?.Dispose();
        _quotaServiceScope?.Dispose();
    }
}