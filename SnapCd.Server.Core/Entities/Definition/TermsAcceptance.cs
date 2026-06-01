// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Records user acceptance of Terms of Service for audit purposes.
/// This is an immutable record - once created, it should not be modified.
/// </summary>
public class TermsAcceptance
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Version of the terms that were accepted (format: YYYY-MM-DD).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string TermsVersion { get; set; }

    /// <summary>
    /// Context in which the terms were accepted.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string AcceptanceContext { get; set; }

    /// <summary>
    /// UTC timestamp when the user accepted the terms.
    /// </summary>
    public DateTime AcceptedDateTime { get; set; }

    /// <summary>
    /// IP address of the user at time of acceptance (for audit).
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the browser at time of acceptance (for audit).
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Constants for AcceptanceContext values.
/// </summary>
public static class TermsAcceptanceContext
{
    public const string Registration = "Registration";
    public const string InvitationRegistration = "InvitationRegistration";
    public const string TermsUpdate = "TermsUpdate";
}
