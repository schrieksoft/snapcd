// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ServicePrincipalFederatedCredentials;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Mappers;

public static class ServicePrincipalFederatedCredentialMapper
{
    public static ServicePrincipalFederatedCredential ToEntity(ServicePrincipalFederatedCredentialCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalFederatedCredential
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            Name = dto.Name,
            Issuer = dto.Issuer,
            Subject = dto.Subject,
            Audience = dto.Audience,
            IsDisabled = dto.IsDisabled
        };
    }

    public static ServicePrincipalFederatedCredentialReadDto ToDto(ServicePrincipalFederatedCredential entity)
    {
        return new ServicePrincipalFederatedCredentialReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            Name = entity.Name,
            Issuer = entity.Issuer,
            Subject = entity.Subject,
            Audience = entity.Audience,
            IsDisabled = entity.IsDisabled
        };
    }

    public static void UpdateEntity(ServicePrincipalFederatedCredential entity, ServicePrincipalFederatedCredentialUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.Name = dto.Name;
        entity.Issuer = dto.Issuer;
        entity.Subject = dto.Subject;
        entity.Audience = dto.Audience;
        entity.IsDisabled = dto.IsDisabled;
    }

    public static ServicePrincipalFederatedCredentialMetadataReadDto ToMetadataDto(ServicePrincipalFederatedCredentialMetadata view)
    {
        return new ServicePrincipalFederatedCredentialMetadataReadDto
        {
            Id = view.Id,
            Name = view.Name,
            ServicePrincipalId = view.ServicePrincipalId
        };
    }
}
