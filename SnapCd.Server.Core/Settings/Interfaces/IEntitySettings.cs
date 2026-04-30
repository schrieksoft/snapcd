namespace SnapCd.Server.Core.Settings.Interfaces;

public interface IEntitySettings
{
    bool EmitCreateEvents { get; set; }
    bool EmitUpdateEvents { get; set; }
    bool EmitDeleteEvents { get; set; }

    /// <summary>
    /// Time-to-live for emitted events. Default is 30 minutes.
    /// </summary>
    TimeSpan EventTtl { get; set; }
}