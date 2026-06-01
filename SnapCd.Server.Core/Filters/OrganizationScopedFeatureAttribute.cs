// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Filters;

/// <summary>
/// Surface-category marker: the controller or page is a billable product feature
/// (Modules / Namespaces / Stacks / Runners / Jobs / Logs / source-change webhook /
/// etc.). Each consumer interprets the marker for itself; the SaaS subscription
/// filter, for example, gates these endpoints behind an active cloud subscription
/// while the CE host registers no consumer at all.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class OrganizationScopedFeatureAttribute : Attribute;
