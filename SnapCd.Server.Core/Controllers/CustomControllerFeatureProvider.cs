// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SnapCd.Server.Core.Controllers;

public class CustomControllerFeatureProvider : ControllerFeatureProvider
{
    private readonly HashSet<Type> _controllerTypes;

    public CustomControllerFeatureProvider(IEnumerable<Type> controllerTypes)
    {
        _controllerTypes = new HashSet<Type>(controllerTypes);
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        // Check if the type is one of the specified controllers
        return _controllerTypes.Contains(typeInfo.AsType()) && base.IsController(typeInfo);
    }
}