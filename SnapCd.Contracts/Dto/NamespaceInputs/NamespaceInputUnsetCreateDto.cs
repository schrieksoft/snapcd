// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespaceInputs.Base;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

/// <summary>DTO for creating a new NamespaceInputUnset (POST operations).</summary>
/// <summary>Unsets a name for this Namespace and every Namespace and Module below it: an input of that name declared on a Namespace above is not passed on. It carries no value of its own.</summary>
public class NamespaceInputUnsetCreateDto : NamespaceInputCreateDto
{

}
