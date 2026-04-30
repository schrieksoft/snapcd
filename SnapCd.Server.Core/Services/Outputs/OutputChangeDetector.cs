using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;

namespace SnapCd.Server.Core.Services.Outputs;

/// <summary>
/// Detects changes between OutputSets by comparing individual outputs.
/// </summary>
public static class OutputChangeDetector
{
    /// <summary>
    /// Detects which outputs were created or updated compared to the previous OutputSet.
    /// </summary>
    /// <param name="previous">The previous OutputSet for the same module (null if this is the first).</param>
    /// <param name="current">The current OutputSet being created.</param>
    /// <param name="secretChanges">Dictionary mapping secret output names to whether their vault value was changed.</param>
    /// <returns>List of output names that were created or updated.</returns>
    public static List<string> DetectChanges(
        OutputSet? previous,
        OutputSet current,
        Dictionary<string, bool> secretChanges)
    {
        var changedOutputs = new List<string>();

        foreach (var output in current.Outputs)
        {
            var previousOutput = previous?.Outputs.FirstOrDefault(o => o.Name == output.Name);

            if (previousOutput == null)
            {
                // New output - didn't exist in previous OutputSet
                changedOutputs.Add(output.Name);
                continue;
            }

            switch (output)
            {
                case LiteralOutput literal:
                    var prevLiteral = previousOutput as LiteralOutput;
                    if (prevLiteral == null || prevLiteral.Value != literal.Value)
                    {
                        // Value changed or type changed from secret to literal
                        changedOutputs.Add(output.Name);
                    }
                    break;

                case SecretOutput:
                    if (secretChanges.TryGetValue(output.Name, out var wasChanged) && wasChanged)
                    {
                        // Vault secret was created or updated
                        changedOutputs.Add(output.Name);
                    }
                    break;
            }
        }

        return changedOutputs;
    }
}
