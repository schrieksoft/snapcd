using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Views;

public class GeneratedToken
{
    [MaxLength(2000)] public string TokenString { get; set; } = null!;
    public Token TokenEntity { get; set; } = null!;
}