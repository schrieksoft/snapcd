using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Views;

public class TerraformModuleInfo
{
    [MaxLength(2000)] public string Key { get; set; } = null!;
    [MaxLength(2000)] public string Source { get; set; } = null!;
    [MaxLength(2000)] public string Dir { get; set; } = null!;
}