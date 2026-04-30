namespace SnapCd.Server.Core.Views;

public class TruncatedNamespace
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required List<TruncatedModule> Modules { get; set; }
}