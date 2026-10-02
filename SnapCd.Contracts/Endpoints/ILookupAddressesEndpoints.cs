// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.StateMigrations;

namespace SnapCd.Contracts.Endpoints;

/// <summary>
/// The steps a runner must service to list what is in a Module's state.
///
/// The server dispatches by member name and the runner implements the interface, so a step
/// the server can send is one the runner has a handler for.
/// </summary>
public interface ILookupAddressesEndpoints
{
    Task LookupAddresses(LookupAddressesRequestBase request);

    Task LookupAddressesGetModule(GetModuleRequestBase request);

    Task LookupAddressesInit(InitRequestBase request);
}
