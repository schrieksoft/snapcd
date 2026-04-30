namespace SnapCd.Runner.Utils;

public static class StringFormatting
{
    public static string EscapeBashScript(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}

public static class PathUtils
{
    /// <summary>
    /// Expands tilde (~) at the start of a path to the user's home directory.
    /// </summary>
    public static string ExpandTilde(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (path.StartsWith("~/") || path == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return path == "~" ? home : Path.Combine(home, path.Substring(2));
        }

        return path;
    }
}