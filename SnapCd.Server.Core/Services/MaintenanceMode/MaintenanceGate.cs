// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Services.MaintenanceMode;

/// <summary>
/// Static write gate consulted by GenericRepository. Static because repositories are frequently
/// constructed by hand rather than resolved from the container; the host initializes the gate
/// once at startup so every repository instance is covered regardless of how it was built.
/// Uninitialized (tools, tests) the gate allows everything.
/// </summary>
public static class MaintenanceGate
{
    private static IMaintenanceModeService? _service;

    public static void Initialize(IMaintenanceModeService service) => _service = service;

    public static void Reset() => _service = null;

    public static async Task EnsureWriteAllowedAsync()
    {
        if (_service == null || CallerContext.CallerContext.IsExempt) return;

        if (await _service.IsActiveAsync())
            throw new MaintenanceModeException("A maintenance window is open; changes cannot be saved until it closes.");
    }
}
