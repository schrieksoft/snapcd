// using System.Text.Json;
//
// namespace SnapCd.Server.Host.Services.ResolvedConfiguration.HelperClasses;
//
// public class Diff
// {
//     public string PropertyPath { get; set; } // e.g., "ModuleName", "NamespaceParams[0]"
//     public JsonElement? AppliedValue { get; set; } // Null if the property is newly added
//     public JsonElement? DeclaredValue { get; set; } // Null if the property is removed
//
//     public override string ToString()
//     {
//         var oldValue = AppliedValue?.ToString() ?? "null";
//         var newValue = DeclaredValue?.ToString() ?? "null";
//         return $"{PropertyPath}: {oldValue} → {newValue}";
//     }
// }

