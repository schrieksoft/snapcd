// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Misc.Utils.Helpers;

public static class AsyncEnumerableExtensions
{
    public static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return ExecuteAsync();

        async Task<List<T>> ExecuteAsync()
        {
            var list = new List<T>();

            await foreach (var element in source) list.Add(element);

            return list;
        }
    }
}