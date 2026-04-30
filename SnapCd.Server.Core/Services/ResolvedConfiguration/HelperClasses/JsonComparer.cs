// using System.Text.Json;
//
// namespace SnapCd.Server.SelfHosted.Services.ResolvedConfiguration.HelperClasses;
//
// public static class JsonComparer
// {
//     public static List<Diff> GetDifferences(string appliedJson, string declaredJson)
//     {
//         var diffs = new List<Diff>();
//
//         var applied = JsonDocument.Parse(appliedJson);
//         var declared = JsonDocument.Parse(declaredJson);
//
//         CompareJsonElements("", applied.RootElement, declared.RootElement, diffs);
//
//         return diffs;
//     }
//
//     private static void CompareJsonElements(string path, JsonElement applied, JsonElement declared, List<Diff> diffs)
//     {
//         if (applied.ValueKind != declared.ValueKind)
//         {
//             diffs.Add(new Diff { PropertyPath = path, AppliedValue = applied, DeclaredValue = declared });
//             return;
//         }
//
//         switch (applied.ValueKind)
//         {
//             case JsonValueKind.Object:
//                 var properties1 = new Dictionary<string, JsonElement>();
//                 foreach (var prop in applied.EnumerateObject()) properties1[prop.Name] = prop.Value;
//
//                 var properties2 = new Dictionary<string, JsonElement>();
//                 foreach (var prop in declared.EnumerateObject()) properties2[prop.Name] = prop.Value;
//
//                 foreach (var prop in properties1.Keys)
//                     if (!properties2.ContainsKey(prop))
//                         // Property removed
//                         diffs.Add(new Diff
//                         {
//                             PropertyPath = $"{path}.{prop}".Trim('.'), AppliedValue = properties1[prop],
//                             DeclaredValue = null
//                         });
//                     else
//                         CompareJsonElements($"{path}.{prop}".Trim('.'), properties1[prop], properties2[prop], diffs);
//
//                 foreach (var prop in properties2.Keys)
//                     if (!properties1.ContainsKey(prop))
//                         // Property added
//                         diffs.Add(new Diff
//                         {
//                             PropertyPath = $"{path}.{prop}".Trim('.'), AppliedValue = null,
//                             DeclaredValue = properties2[prop]
//                         });
//
//                 break;
//
//             case JsonValueKind.Array:
//                 // TODO probably need to make this more intelligent so as to allow for changing indexes.
//                 var len1 = applied.GetArrayLength();
//                 var len2 = declared.GetArrayLength();
//                 var maxLen = Math.Max(len1, len2);
//
//                 for (var i = 0; i < maxLen; i++)
//                     if (i >= len1)
//                         // New item added
//                         diffs.Add(new Diff
//                             { PropertyPath = $"{path}[{i}]", AppliedValue = null, DeclaredValue = declared[i] });
//                     else if (i >= len2)
//                         // Item removed
//                         diffs.Add(new Diff
//                             { PropertyPath = $"{path}[{i}]", AppliedValue = applied[i], DeclaredValue = null });
//                     else
//                         CompareJsonElements($"{path}[{i}]", applied[i], declared[i], diffs);
//
//                 break;
//
//             default:
//                 if (!AreJsonElementsEqual(applied, declared))
//                     diffs.Add(new Diff { PropertyPath = path, AppliedValue = applied, DeclaredValue = declared });
//                 break;
//         }
//     }
//
//     private static bool AreJsonElementsEqual(JsonElement elem1, JsonElement elem2)
//     {
//         switch (elem1.ValueKind)
//         {
//             case JsonValueKind.String:
//                 return elem1.GetString() == elem2.GetString();
//             case JsonValueKind.Number:
//                 return elem1.GetDecimal() == elem2.GetDecimal();
//             case JsonValueKind.True:
//                 return elem1.GetBoolean() == elem2.GetBoolean();
//             case JsonValueKind.False:
//                 return elem1.GetBoolean() == elem2.GetBoolean();
//             case JsonValueKind.Null:
//                 return elem2.ValueKind == JsonValueKind.Null;
//             case JsonValueKind.Array:
//                 // For Object/Array types, we need to recursively compare each element
//                 return elem1.ToString() == elem2.ToString(); // Simple deep comparison for objects/arrays
//             default:
//                 return elem1.ToString() == elem2.ToString(); // Fallback for other cases
//         }
//     }
// }

