using System.Text.RegularExpressions;

namespace SnapCd.Server.Core.Views;

public enum TerraformModuleSource
{
    GitRepository,
    DownloadedLocalPath,
    LocalPath,
    TerraformRegistry,
    Unknown
}

public static class SourceHelper
{
    public static TerraformModuleSource CategorizeSource(string source, string workingDir, string prefix)
    {
        // Check if it's a git repo
        if (IsGitRepo(source)) return TerraformModuleSource.GitRepository;

        // Check for a local path
        if (IsLocalPath(workingDir, source))
        {
            if (prefix == "")
                return TerraformModuleSource.LocalPath;
            return TerraformModuleSource.DownloadedLocalPath;
        }

        // Check if it's a terraform registry
        if (IsTerraformRegistry(source)) return TerraformModuleSource.TerraformRegistry;

        return TerraformModuleSource.Unknown;
    }

    private static bool IsLocalPath(string workingDir, string source)
    {
        // Check if the source is a local file or directory path
        var path = Path.Combine(workingDir, source);
        return Directory.Exists(path) || File.Exists(path);
    }

    private static bool IsGitRepo(string source)
    {
        // Check if the source matches the common git URL formats
        var gitPattern = @"^(https:\/\/|git@|ssh:\/\/|github\.com|git:\/\/|bitbucket\.org)";
        return Regex.IsMatch(source, gitPattern, RegexOptions.IgnoreCase);
    }

    private static bool IsTerraformRegistry(string source)
    {
        // Check if the source matches the Terraform Registry module format
        // Format: <namespace>/<name>/<provider>
        var terraformRegistryPattern = @"^[\w\-]+\/[\w\-]+\/[\w\-]+$";
        return Regex.IsMatch(source, terraformRegistryPattern);
    }
}