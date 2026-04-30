using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Entities.Definition;

public class LiteralOutput : Output
{
    [MaxLength(32000)] public string Value { get; set; } = null!;
}