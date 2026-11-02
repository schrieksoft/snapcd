// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

/// <summary>
/// Verifies at startup that the configured working and temp directories can be created and written
/// to. A mounted directory carries its host ownership, which the Runner's uid may not match.
/// </summary>
public static class WorkingDirectoryCheck
{
    public static void Verify(WorkingDirectorySettings settings)
    {
        foreach (var path in new[] { settings.WorkingDirectory, settings.TempDirectory })
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            try
            {
                Directory.CreateDirectory(path);

                var probe = Path.Combine(path, $".writable-{Guid.NewGuid():N}");
                File.WriteAllBytes(probe, []);
                File.Delete(probe);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"The Runner cannot write to '{path}'. It runs as {Describe()}, and a mounted " +
                    "directory keeps the ownership it has on the host. Make the directory writable by " +
                    "that user, or mount a volume that the container initialises itself.", ex);
            }
        }
    }

    private static string Describe()
    {
        var uid = OperatingSystem.IsWindows() ? null : GetEffectiveUid();
        return uid is null ? Environment.UserName : $"{Environment.UserName} (uid {uid})";
    }

    private static string? GetEffectiveUid()
    {
        try
        {
            // /proc/self/status reports the real, effective, saved and filesystem uids.
            var line = File.ReadLines("/proc/self/status").FirstOrDefault(l => l.StartsWith("Uid:"));
            return line?.Split('\t', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
        }
        catch
        {
            return null;
        }
    }
}
