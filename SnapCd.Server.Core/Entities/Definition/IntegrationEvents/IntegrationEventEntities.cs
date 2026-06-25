// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.IntegrationEvents;

// The "demand" side: a scope subscribes a trigger to an integration. Mirrors the Mission entities, swapping
// AgentId→IntegrationId and MissionType→Trigger, plus an optional template + filter.

public class OrganizationIntegrationEvent : AuditBase, IEntity, IOrganizationChild, IIntegrationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid IntegrationId { get; set; }
    public IntegrationTrigger Trigger { get; set; }
    [MaxLength(8000)] public string? Template { get; set; }
    [MaxLength(2000)] public string? Filter { get; set; }
    public bool IsDisabled { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public Integration Integration { get; set; } = null!;

    public Guid ParentId() => IntegrationId;
}

public class StackIntegrationEvent : AuditBase, IEntity, IOrganizationChild, IStackChild, IIntegrationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid IntegrationId { get; set; }
    public Guid StackId { get; set; }
    public IntegrationTrigger Trigger { get; set; }
    [MaxLength(8000)] public string? Template { get; set; }
    [MaxLength(2000)] public string? Filter { get; set; }
    public bool IsDisabled { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public Integration Integration { get; set; } = null!;
    [JsonIgnore] public Stack Stack { get; set; } = null!;

    public Guid ParentId() => StackId;
}

public class NamespaceIntegrationEvent : AuditBase, IEntity, IOrganizationChild, INamespaceChild, IIntegrationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid IntegrationId { get; set; }
    public Guid NamespaceId { get; set; }
    public IntegrationTrigger Trigger { get; set; }
    [MaxLength(8000)] public string? Template { get; set; }
    [MaxLength(2000)] public string? Filter { get; set; }
    public bool IsDisabled { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public Integration Integration { get; set; } = null!;
    [JsonIgnore] public Namespace Namespace { get; set; } = null!;

    public Guid ParentId() => NamespaceId;
}

public class ModuleIntegrationEvent : AuditBase, IEntity, IOrganizationChild, IModuleChild, IIntegrationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid IntegrationId { get; set; }
    public Guid ModuleId { get; set; }
    public IntegrationTrigger Trigger { get; set; }
    [MaxLength(8000)] public string? Template { get; set; }
    [MaxLength(2000)] public string? Filter { get; set; }
    public bool IsDisabled { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public Integration Integration { get; set; } = null!;
    [JsonIgnore] public Module Module { get; set; } = null!;

    public Guid ParentId() => ModuleId;
}
