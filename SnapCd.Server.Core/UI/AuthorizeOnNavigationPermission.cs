// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.UI;

/// <summary>
/// Marks a Razor page so that <see cref="Dashboard.Layout.OrganizationMainLayout"/> and
/// <see cref="Dashboard.Layout.NavMenu"/> evaluate role-based access for both rendering
/// the page and showing its navigation entry. With no roles, the marker only opts the
/// page into navigation visibility (everyone authenticated may navigate to it). With
/// one or more roles, the page is restricted to users holding any of them in the
/// current organization (or a System Administrator).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AuthorizeOnNavigationPermission : Attribute
{
    public OrganizationRole[] AnyOf { get; }

    public AuthorizeOnNavigationPermission(params OrganizationRole[] anyOf)
    {
        AnyOf = anyOf;
    }
}
