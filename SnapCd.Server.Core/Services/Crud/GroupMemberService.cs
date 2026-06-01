// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Mappers.GroupMembers.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.GroupMembers;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud;

public class GroupMemberServiceFactory(
    GroupMemberSecuredRepositoryFactory groupMemberSecuredRepositoryFactory,
    ServicePrincipalGroupMemberSecuredRepositoryFactory servicePrincipalGroupMemberSecuredRepositoryFactory,
    UserGroupMemberSecuredRepositoryFactory userGroupMemberSecuredRepositoryFactory,
    GroupGroupMemberSecuredRepositoryFactory groupGroupMemberSecuredRepositoryFactory)
{
    public GroupMemberService Create(IPrincipalProvider? principalProvider = null)
    {
        var groupMemberRepo = groupMemberSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalGroupMemberSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userGroupMemberSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupGroupMemberSecuredRepositoryFactory.Create(principalProvider);

        return new GroupMemberService(
            groupMemberRepo,
            servicePrincipalRepo,
            userRepo,
            groupRepo);
    }
}

public class GroupMemberService : IDisposable
{
    protected readonly GroupMemberSecuredRepository GroupMemberSecuredRepository;
    protected readonly GroupGroupMemberSecuredRepository GroupGroupMemberSecuredRepository;
    protected readonly UserGroupMemberSecuredRepository UserGroupMemberSecuredRepository;
    protected readonly ServicePrincipalGroupMemberSecuredRepository ServicePrincipalGroupMemberSecuredRepository;

    public GroupMemberService(
        GroupMemberSecuredRepository groupMemberSecuredRepository,
        ServicePrincipalGroupMemberSecuredRepository servicePrincipalGroupMemberSecuredRepository,
        UserGroupMemberSecuredRepository userGroupMemberSecuredRepository,
        GroupGroupMemberSecuredRepository groupGroupMemberSecuredRepository
    )
    {
        GroupMemberSecuredRepository = groupMemberSecuredRepository;
        ServicePrincipalGroupMemberSecuredRepository = servicePrincipalGroupMemberSecuredRepository;
        UserGroupMemberSecuredRepository = userGroupMemberSecuredRepository;
        GroupGroupMemberSecuredRepository = groupGroupMemberSecuredRepository;
    }

    protected virtual GroupMember MapToEntity(GroupMemberCreateDto dto, Guid organizationId)
    {
        return GroupMemberMapper.ToEntity(dto, organizationId);
    }

    protected virtual GroupMemberReadDto MapToDto(GroupMember entity)
    {
        return GroupMemberMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(GroupMember entity, GroupMemberUpdateDto dto)
    {
        GroupMemberMapper.UpdateEntity(entity, dto);
    }


    public virtual void Dispose()
    {
        GroupMemberSecuredRepository.Dispose();
        ServicePrincipalGroupMemberSecuredRepository.Dispose();
        ;
        UserGroupMemberSecuredRepository.Dispose();
        ;
        GroupGroupMemberSecuredRepository.Dispose();
        ;
    }


    public virtual async Task<GroupMemberReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await GroupMemberSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<GroupMemberReadDto>> List(Guid organizationId)
    {
        var entities = await GroupMemberSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<GroupMemberReadDto> Create(GroupMemberCreateDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.GroupMemberDiscriminator switch
        {
            GroupMemberDiscriminator.User => await UserGroupMemberSecuredRepository.Create((UserGroupMember)entity),
            GroupMemberDiscriminator.ServicePrincipal => await ServicePrincipalGroupMemberSecuredRepository.Create((ServicePrincipalGroupMember)entity),
            GroupMemberDiscriminator.Group => await GroupGroupMemberSecuredRepository.Create((GroupGroupMember)entity),
            _ => throw new ArgumentException($"Unknown GroupMemberDiscriminator: {dto.GroupMemberDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<GroupMemberReadDto> Update(GroupMemberUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await GroupMemberSecuredRepository.Get(id, organizationId);

        if (entity.GroupMemberDiscriminator != dto.GroupMemberDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change GroupMemberDiscriminator from {entity.GroupMemberDiscriminator} to {dto.GroupMemberDiscriminator}. " +
                "Delete the existing member and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.GroupMemberDiscriminator switch
        {
            GroupMemberDiscriminator.User => await UserGroupMemberSecuredRepository.Update((UserGroupMember)entity),
            GroupMemberDiscriminator.ServicePrincipal => await ServicePrincipalGroupMemberSecuredRepository.Update((ServicePrincipalGroupMember)entity),
            GroupMemberDiscriminator.Group => await GroupGroupMemberSecuredRepository.Update((GroupGroupMember)entity),
            _ => throw new ArgumentException($"Unknown GroupMemberDiscriminator: {entity.GroupMemberDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await GroupMemberSecuredRepository.Delete(id, organizationId);
    }
}