// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

/// <summary>
/// Named SignalR methods the server invokes on a connected SnapCd.Agent — one per mission type,
/// mirroring <see cref="RunnerEndpoints"/> (one constant per runner task). Each endpoint has its
/// own request DTO under <c>SnapCd.Contracts.AgentRequests</c> and its own orchestrator-side
/// handler. There is no single generic invocation endpoint.
/// </summary>
public static class AgentEndpoints
{
    public const string AutoDiagnose = "AutoDiagnose";
    public const string ApprovalRecommend = "ApprovalRecommend";
    public const string SummarizeJob = "SummarizeJob";
    public const string AutoFix = "AutoFix";

    /// <summary>Server → agent: cancel an in-flight mission run (the agent twin of
    /// <c>RunnerEndpoints.CancelGraceful</c>). Carries a <c>CancelMissionRequest</c>.</summary>
    public const string CancelMission = "CancelMission";
    public const string Ping = "Ping";
}
