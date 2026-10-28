// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Misc.Utils.Helpers;

public static class AttributeHelper
{
    public static IEnumerable<KeyValuePair<string, object>> ParseAttributes(string attributes)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(attributes))
        {
            var parts = attributes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0];
                    var value = keyValue[1].Trim('"');
                    result.Add(key, value);
                }
            }
        }

        return result;
    }
}