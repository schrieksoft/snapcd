// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Validation;

public class ServiceBusSettingsValidator : IValidateOptions<ServiceBusSettings>
{
    public ValidateOptionsResult Validate(string? name, ServiceBusSettings options)
    {
        if (options.BusType == BusType.AzureServiceBus)
        {
            if (options.TransportOptions.AzureServiceBus == null)
                return ValidateOptionsResult.Fail("ServiceBus:TransportOptions:AzureServiceBus is required when ServiceBus:BusType is 'AzureServiceBus'.");

            if (string.IsNullOrEmpty(options.TransportOptions.AzureServiceBus.ConnectionString))
                return ValidateOptionsResult.Fail("ServiceBus:TransportOptions:AzureServiceBus:ConnectionString is required when ServiceBus:BusType is 'AzureServiceBus'.");
        }

        return ValidateOptionsResult.Success;
    }
}
