// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Services.Integrations.Codecs;

public sealed class IntegrationCodecRegistry : IIntegrationCodecRegistry
{
    private readonly IReadOnlyDictionary<IntegrationType, IIntegrationCodec> _codecs;

    public IntegrationCodecRegistry(IEnumerable<IIntegrationCodec> codecs)
        => _codecs = codecs.ToDictionary(c => c.Type);

    public IIntegrationCodec Get(IntegrationType type)
        => _codecs.TryGetValue(type, out var codec)
            ? codec
            : throw new NotSupportedException($"No integration codec registered for type '{type}'.");
}
