namespace SnapCd.Server.Core.Views;

public class TruncatedStack
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required List<TruncatedNamespace> Namespaces { get; set; }
}