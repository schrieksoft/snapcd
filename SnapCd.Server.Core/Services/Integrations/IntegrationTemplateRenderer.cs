// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// Minimal, sandboxed template rendering: <c>{{ token }}</c> substitution only (no code execution). Unknown
/// tokens render empty. A built-in default template per trigger is used when an event supplies none.
/// </summary>
public static class IntegrationTemplateRenderer
{
    private static readonly Regex TokenPattern = new(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    public static string Render(string template, IReadOnlyDictionary<string, string?> context)
        => TokenPattern.Replace(template, m => context.TryGetValue(m.Groups[1].Value, out var v) ? v ?? string.Empty : string.Empty);

    public static string DefaultTemplate(IntegrationTrigger trigger) => trigger switch
    {
        IntegrationTrigger.JobFailed => "❌ {{jobType}} failed on *{{moduleName}}* ({{stackName}}/{{namespaceName}})\n{{jobUrl}}",
        IntegrationTrigger.JobSucceeded => "✅ {{jobType}} succeeded on *{{moduleName}}* ({{stackName}}/{{namespaceName}})\n{{jobUrl}}",
        IntegrationTrigger.JobAwaitingApproval => "⏳ *{{moduleName}}* is awaiting approval ({{stackName}}/{{namespaceName}})\n{{jobUrl}}",
        IntegrationTrigger.JobCancelled => "🚫 {{jobType}} on *{{moduleName}}* was cancelled ({{stackName}}/{{namespaceName}})\n{{jobUrl}}",
        IntegrationTrigger.JobApproved => "👍 {{jobType}} on *{{moduleName}}* was approved ({{stackName}}/{{namespaceName}})\n{{jobUrl}}",
        IntegrationTrigger.JobDeclined => "👎 {{jobType}} on *{{moduleName}}* was declined ({{stackName}}/{{namespaceName}})\n{{jobUrl}}",
        IntegrationTrigger.MissionStarted => "🛰️ {{missionType}} started on *{{moduleName}}* ({{stackName}}/{{namespaceName}})",
        IntegrationTrigger.MissionMilestoneReported => "🛰️ {{missionType}} on *{{moduleName}}* ({{stackName}}/{{namespaceName}}): {{message}}",
        IntegrationTrigger.MissionCompleted => "🏁 {{missionType}} completed on *{{moduleName}}* ({{stackName}}/{{namespaceName}})",
        IntegrationTrigger.MissionFaulted => "💥 {{missionType}} faulted on *{{moduleName}}* ({{stackName}}/{{namespaceName}})",
        _ => "{{trigger}} on *{{moduleName}}* ({{stackName}}/{{namespaceName}})\n{{jobUrl}}"
    };
}
