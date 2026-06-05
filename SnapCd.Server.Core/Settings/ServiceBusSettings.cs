// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using MassTransit;
using MassTransit.SqlTransport;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Configuration for the MassTransit service bus that carries internal events between Server
/// components (saga endpoints, runner / agent dispatch, log fanout, etc.). Two transports are
/// supported, selected by <see cref="BusType"/>:
///
/// <list type="bullet">
///   <item>SqlServer — the default. Routes events through SQL Server tables sharing the
///         application database. Suitable for single-region deployments up to moderate scale;
///         requires no additional infrastructure.</item>
///   <item>AzureServiceBus — routes events through an Azure Service Bus namespace. Suitable
///         for higher-throughput deployments and any topology that wants the bus on managed
///         infrastructure separate from SQL Server.</item>
/// </list>
/// </summary>
public class ServiceBusSettings
{
    /// <summary>
    /// Prefix prepended to every consumer queue name. Useful for sharing one bus across multiple
    /// Snap CD deployments (each with a distinct prefix) without queue-name collisions.
    /// </summary>
    public string EndpointsPrefix { get; set; } = "";

    /// <summary>
    /// When true, the consumer's .NET namespace is included in the generated queue name in
    /// addition to <see cref="EndpointsPrefix"/>. Defaults to false.
    /// </summary>
    public bool EndpointsPrefixIncludeNameSpace { get; set; } = false;

    /// <summary>
    /// Which transport carries internal events. SqlServer is the default; AzureServiceBus is the
    /// alternative for higher-throughput deployments.
    /// </summary>
    public BusType BusType { get; set; } = BusType.SqlServer;

    /// <summary>
    /// Maximum number of concurrent messages processed by saga endpoints. Higher values can
    /// improve throughput but increase database load.
    /// </summary>
    public int SagaConcurrencyLimit { get; set; } = 20;

    /// <summary>
    /// Per-transport configuration. The block matching <see cref="BusType"/> is the only one
    /// consumed; the other is ignored. Both transports' option types come straight from MassTransit
    /// (<see cref="AzureServiceBusTransportOptions"/> / <see cref="SqlTransportOptions"/>) so the
    /// full MassTransit-side surface remains available to operators — the generated JSON Schema
    /// documents the operator-relevant fields via hand-authored fragments.
    /// </summary>
    public TransportOptionsSettings TransportOptions { get; set; } = new();
}

/// <summary>
/// Per-transport settings. The block matching <see cref="ServiceBusSettings.BusType"/> is read
/// at startup; the other block is ignored. Each property's type is the corresponding MassTransit
/// option class so binding semantics and forwards-compatibility with MassTransit upgrades match
/// what MassTransit consumers expect. The properties are <see cref="JsonIgnoreAttribute"/>'d only
/// because the BCL JSON Schema exporter recurses infinitely on those types' nested-metadata
/// graphs; runtime config binding via <c>Microsoft.Extensions.Configuration.Binder</c> is
/// unaffected by <c>[JsonIgnore]</c>, and the generator injects hand-authored schemas in place
/// of the empty positions.
/// </summary>
public class TransportOptionsSettings
{
    /// <summary>
    /// Azure Service Bus transport configuration. Required when
    /// <see cref="ServiceBusSettings.BusType"/> is AzureServiceBus.
    /// </summary>
    [JsonIgnore]
    public AzureServiceBusTransportOptions? AzureServiceBus { get; set; }

    /// <summary>
    /// SQL Server transport configuration. Required when <see cref="ServiceBusSettings.BusType"/>
    /// is SqlServer. Uses MassTransit's <see cref="SqlTransportOptions"/> directly so the full
    /// surface (admin credentials, port, connection limit, etc.) is operator-tunable.
    /// </summary>
    [JsonIgnore]
    public SqlTransportOptions? SqlServer { get; set; }
}
