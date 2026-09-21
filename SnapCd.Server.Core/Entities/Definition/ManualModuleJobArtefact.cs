// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// A file one participant's slice produced that another participant's slice needs: a state
/// fragment, or the planned output values a consumer's plan depends on.
///
/// Job-scoped and encrypted with the same service that encrypts state files, because a fragment is
/// raw state and can carry anything state carries. The saga deletes these from every terminal path;
/// nothing here is a durable record.
/// </summary>
public class ManualModuleJobArtefact : AuditBase, IEntity
{
    /// <summary>The artefact's id.</summary>
    public Guid Id { get; set; }

    /// <summary>The organization the job belongs to.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>The job that produced it; artefacts never outlive their job.</summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// What it is, as the runner names it: "fragment-app.tfstate", "outputs-network.yaml". Unique
    /// per job, so a re-run of a slice replaces rather than accumulates.
    /// </summary>
    [MaxLength(255)] public string Name { get; set; } = null!;

    /// <summary>The file's bytes, encrypted. Never logged, never returned to a page.</summary>
    public string Ciphertext { get; set; } = null!;

    [JsonIgnore] public ManualModuleJob Job { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return JobId;
    }
}
