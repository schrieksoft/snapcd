using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Views;

public class TerraformModule
{
    [MaxLength(2000)] public string DownloadToDir { get; set; } = string.Empty;

    [MaxLength(2000)] public string RepoSource { get; set; } = string.Empty;

    [MaxLength(255)] public string RepoRevision { get; set; } = string.Empty;

    public TerraformModuleSource SourceType { get; set; }

    public TerraformModuleInfo TerraformModuleInfo { get; set; } = null!;
}