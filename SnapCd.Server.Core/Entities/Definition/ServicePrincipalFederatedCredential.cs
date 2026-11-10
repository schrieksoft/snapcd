// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// A trust relationship that lets a workload authenticate as a Service Principal by presenting a
/// token its own platform issued, instead of a client secret. The token is accepted when its
/// issuer, subject and audience all match this record and its signature verifies against the
/// issuer's published keys.
/// </summary>
public class ServicePrincipalFederatedCredential : AuditBase, IEntity, IOrganizationChild, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ServicePrincipalId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    /// <summary>
    /// The `iss` the presented token must carry, which is also where its signing keys are
    /// discovered. An absolute https URL.
    /// </summary>
    [MaxLength(2048)] public string Issuer { get; set; } = null!;

    /// <summary>
    /// The `sub` the presented token must carry. For a Kubernetes ServiceAccount this is
    /// `system:serviceaccount:{namespace}:{name}`.
    /// </summary>
    [MaxLength(1024)] public string Subject { get; set; } = null!;

    /// <summary>The `aud` the presented token must carry.</summary>
    [MaxLength(255)] public string Audience { get; set; } = null!;

    public bool IsDisabled { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual ServicePrincipal ServicePrincipal { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
