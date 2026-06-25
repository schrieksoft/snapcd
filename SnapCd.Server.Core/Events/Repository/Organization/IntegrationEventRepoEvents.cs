// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Events.Repository.Organization.Base;

namespace SnapCd.Server.Core.Events.Repository.Organization;

// Shared across the four scope tables; the DTO's Scope field distinguishes which.
public class IntegrationEventCreatedEvent : CreatedEvent<IntegrationEventDto>;

public class IntegrationEventUpdatedEvent : UpdatedEvent<IntegrationEventDto>;

public class IntegrationEventDeletedEvent : DeletedEvent<IntegrationEventDto>;
