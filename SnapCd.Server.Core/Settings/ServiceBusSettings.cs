// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Settings;

public class ServiceBusSettings
{
    public string EndpointsPrefix { get; set; } = "";
    public bool EndpointsPrefixIncludeNameSpace { get; set; } = false;

    public BusType BusType { get; set; } = BusType.SqlServer;

    /// <summary>
    /// Maximum number of concurrent messages processed by saga endpoints.
    /// Higher values can improve throughput but increase database load.
    /// </summary>
    public int SagaConcurrencyLimit { get; set; } = 20;

    public MassTransitTransportOptions TransportOptions { get; set; } = new();
}

public class MassTransitTransportOptions
{
    public AzureServiceBusTransportOptions? AzureServiceBus { get; set; }

    public RabbitMqTransportOptions? RabbitMq { get; set; }

    public AmazonSqsTransportOptions? AmazonSqs { get; set; }

    public SqlServerTransportOptions? SqlServer { get; set; }
}

public class SqlServerTransportOptions
{
    /// <summary>
    /// SQL Server connection string for the MassTransit transport queues. If null, the app's
    /// primary ConnectionString is reused (share the SnapCd database).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Schema used for transport tables. Defaults to "transport".
    /// </summary>
    public string Schema { get; set; } = "transport";
}