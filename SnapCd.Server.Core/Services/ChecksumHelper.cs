using System.Security.Cryptography;
using System.Text;

namespace SnapCd.Server.Core.Services;

public static class ChecksumHelper
{
    public static void SaveChecksum(string folderPath, string checksum)
    {
        var hashFilePath = Path.Combine(folderPath, "checksum.txt");
        File.WriteAllText(hashFilePath, checksum);
    }

    // Helper method to get the latest version's checksum from checksum.txt
    public static string GetLatestChecksum(string folderBaseName, string storagePath)
    {
        var baseDirectoryPath = Path.Combine(storagePath, folderBaseName);

        // Check if the base directory exists
        if (!Directory.Exists(baseDirectoryPath))
            return string.Empty;

        // Get all subfolders (timestamps) and find the one with the largest name
        var subfolders = Directory.GetDirectories(baseDirectoryPath)
            .OrderByDescending(dir => Path.GetFileName(dir))
            .ToList();

        if (!subfolders.Any()) return string.Empty;

        var latestVersionFolderPath = subfolders.First();
        var checksumFilePath = Path.Combine(latestVersionFolderPath, "checksum.txt");

        return File.Exists(checksumFilePath) ? File.ReadAllText(checksumFilePath) : string.Empty;
    }

    public static string ComputeFileChecksum(string filePath)
    {
        using (var sha256 = SHA256.Create())
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public static string ComputeFolderChecksum(string folderPath)
    {
        using (var sha256 = SHA256.Create())
        {
            var fileHashes = new List<string>();

            foreach (var filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                // Skip files in .git directory or any other files you want to exclude
                if (filePath.Contains(Path.Combine(folderPath, ".git")) || filePath.Contains(Path.Combine(folderPath, ".snapcd")))
                    continue;
                var fileHash = ComputeFileChecksum(filePath);
                fileHashes.Add(fileHash);
            }

            // Sort the file hashes to ensure the folder structure/order doesn't affect the hash
            fileHashes.Sort();

            // Combine all file hashes into a single string
            var combinedHashes = string.Join("", fileHashes);

            // Compute the final hash for the folder
            var folderHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedHashes));
            return BitConverter.ToString(folderHash).Replace("-", "").ToLowerInvariant();
        }
    }
}