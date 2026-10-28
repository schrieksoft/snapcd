// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.CallerContext;

public enum CallerKind
{
    Runner,
    Agent,
    System
}

/// <summary>
/// Ambient caller identity for write gating. Absence of a scope means the write is human-driven
/// and subject to the maintenance gate — machine entry points must open a scope explicitly, so
/// the set of Begin call sites is the complete, greppable list of exempt write paths.
/// </summary>
public static class CallerContext
{
    private static readonly AsyncLocal<CallerKind?> Ambient = new();

    public static CallerKind? Kind => Ambient.Value;

    public static bool IsExempt => Ambient.Value != null;

    public static IDisposable Begin(CallerKind kind)
    {
        var previous = Ambient.Value;
        Ambient.Value = kind;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly CallerKind? _previous;
        public Scope(CallerKind? previous) => _previous = previous;
        public void Dispose() => Ambient.Value = _previous;
    }
}
