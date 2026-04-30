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