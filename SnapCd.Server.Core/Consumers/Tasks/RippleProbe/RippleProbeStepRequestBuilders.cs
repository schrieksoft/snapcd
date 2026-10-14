// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Contracts.RunnerRequests.RippleProbe;
using SnapCd.Server.Core.Consumers.Tasks.Builders;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Consumers.Tasks.RippleProbe;

/// <summary>
/// The payloads a ripple probe's steps carry. Identical to the shared builders except that the
/// metadata names a tree of the probe's own, so a probe never shares a working directory with the
/// Module's real work.
/// </summary>
public static class RippleProbeStepRequestBuilders
{
    /// <summary>
    /// Every probe's checkout lives under this one stack segment, so none is a sibling of a real
    /// Module's checkout.
    /// </summary>
    public const string ProbeStackSegment = "_ripple-probes";

    /// <summary>
    /// The Module's own metadata with the path redirected. ModuleDirectoryService builds every
    /// path from StackName, NamespaceName, ModuleName and the source subdirectory, so replacing
    /// the stack segment is enough to move the whole tree. ModuleId is left alone: it is what log
    /// entries and job reporting correlate on, and it still names the real Module.
    ///
    /// Keyed on the Module rather than the job, so probes on one Module share a checkout and the
    /// provider cache in it. A probe writes no state and mutates nothing but the engine artifacts
    /// an init produces, so there is nothing for two of them to corrupt in each other's tree. What
    /// they must not share is the files the probe itself writes, which carry the job id instead.
    /// </summary>
    public static JobMetadata MetadataFor(ResolvedModule declared)
    {
        var metadata = StepRequestBuilders.MetadataFor(declared);
        metadata.StackName = $"{ProbeStackSegment}/{declared.StackName}";
        return metadata;
    }

    /// <summary>
    /// The shared builders with the metadata redirected. Each delegates rather than repeating the
    /// field mapping, so a field added to a shared request reaches a probe without being added
    /// twice.
    /// </summary>
    public static GetModuleRequestBase GetModule(Guid jobId, Guid organizationId, ResolvedModule declared)
    {
        var request = StepRequestBuilders.GetModule(jobId, organizationId, declared);
        request.Metadata = MetadataFor(declared);
        return request;
    }

    public static InitRequestBase Init(
        Guid jobId,
        Guid organizationId,
        ResolvedModule declared,
        Dictionary<string, string> resolvedEnvVars)
    {
        var request = StepRequestBuilders.Init(jobId, organizationId, declared, resolvedEnvVars);
        request.Metadata = MetadataFor(declared);
        return request;
    }

    public static ValidateRequestBase Validate(Guid jobId, Guid organizationId, ResolvedModule declared)
    {
        var request = StepRequestBuilders.Validate(jobId, organizationId, declared);
        request.Metadata = MetadataFor(declared);
        return request;
    }

    public static RippleProbeProbeRequestBase Probe(
        Guid jobId,
        Guid organizationId,
        ResolvedModule declared,
        string inputsJson) => new()
    {
        JobId = jobId,
        OrganizationId = organizationId,
        Metadata = MetadataFor(declared),
        Engine = declared.Engine,
        InputsJson = inputsJson
    };
}
