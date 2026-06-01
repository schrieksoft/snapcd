// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud;

public class OrganizationUserServiceFactory(
    OrganizationUserSecuredRepositoryFactory organizationUserSecuredRepositoryFactory)
{
    public OrganizationUserService Create(IPrincipalProvider? principalProvider = null)
    {
        var organizationUserRepo = organizationUserSecuredRepositoryFactory.Create(principalProvider);
        return new OrganizationUserService(organizationUserRepo);
    }
}

public class OrganizationUserService : IDisposable
{
    protected readonly OrganizationUserSecuredRepository OrganizationUserSecuredRepository;

    public OrganizationUserService(
        OrganizationUserSecuredRepository organizationUserSecuredRepository)
    {
        OrganizationUserSecuredRepository = organizationUserSecuredRepository;
    }

    public virtual void Dispose()
    {
        OrganizationUserSecuredRepository.Dispose();
    }

    public virtual async Task<List<UserViewDto>> List(Guid organizationId)
    {
        var organizationUsers = await OrganizationUserSecuredRepository.ListByOrganizationId(organizationId);
        return organizationUsers.Select(ou => SimpleUserMapper.ToDto(ou.User)).ToList();
    }

    public virtual async Task<UserViewDto> GetByUsername(string username, Guid organizationId)
    {
        var organizationUsers = await OrganizationUserSecuredRepository.List(
            organizationId,
            query => query.Include(ou => ou.User).Where(ou => ou.User.Email == username));

        var organizationUser = organizationUsers.FirstOrDefault();

        if (organizationUser == null)
            throw new EntityNotFoundException($"User with username '{username}' not found in organization.");

        return SimpleUserMapper.ToDto(organizationUser.User);
    }
}