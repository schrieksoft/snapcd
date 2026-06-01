// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Misc.Exceptions;

public class LicenseLimitExceededException : Exception
{
    public string ResourceType { get; }
    public int CurrentUsage { get; }
    public int Limit { get; }
    public int RequestedCount { get; }
    public bool PayAsYouGoEnabled { get; }

    public LicenseLimitExceededException(
        string resourceType,
        int currentUsage,
        int limit,
        int requestedCount,
        string message) : base(message)
    {
        ResourceType = resourceType;
        CurrentUsage = currentUsage;
        Limit = limit;
        RequestedCount = requestedCount;
    }

    public LicenseLimitExceededException(
        string resourceType,
        int currentUsage,
        int limit,
        int requestedCount,
        string message,
        Exception innerException) : base(message, innerException)
    {
        ResourceType = resourceType;
        CurrentUsage = currentUsage;
        Limit = limit;
        RequestedCount = requestedCount;
    }
}