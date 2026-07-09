// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Amazon SES (Simple Email Service) transport credentials and sender identity. Used when
/// <c>EmailSender.EmailProvider</c> is <c>"AmazonSES"</c>; otherwise ignored.
/// </summary>
public class AmazonSesEmailTransportSettings
{
    /// <summary>
    /// IAM access key ID. Ignored when <see cref="UseDefaultCredentials"/> is true (the AWS SDK
    /// then resolves credentials via instance profile / env vars / etc.). Sensitive — source via
    /// the External Settings provider in production.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// IAM secret access key paired with <see cref="AccessKey"/>. Ignored when
    /// <see cref="UseDefaultCredentials"/> is true. Sensitive.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>AWS region the SES endpoint lives in (e.g. <c>eu-north-1</c>, <c>us-east-1</c>).</summary>
    [Required]
    public string Region { get; set; } = null!;

    /// <summary>"From" address every Server-sent email is delivered as. Must be verified in SES.</summary>
    [Required]
    public string FromEmail { get; set; } = null!;

    /// <summary>Display name shown alongside <see cref="FromEmail"/> in clients.</summary>
    [Required]
    public string FromName { get; set; } = null!;

    /// <summary>
    /// When true (default), resolve credentials via the standard AWS SDK chain (instance profile,
    /// env vars, shared credentials file). When false, use <see cref="AccessKey"/> /
    /// <see cref="SecretKey"/> explicitly.
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = true;
}