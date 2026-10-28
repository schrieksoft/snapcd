// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Misc.Utils;

public static class DefinitionParamsHelper
{
    public static Dictionary<string, string> Get<TRequest>(TRequest message)
        where TRequest : StepRequestBase, new()
    {
        return new Dictionary<string, string>
        {
            { "StackId", message.Declared.StackId.ToString() },
            { "StackName", message.Declared.StackName },

            { "NamespaceId", message.Declared.NamespaceId.ToString() },
            { "NamespaceName", message.Declared.NamespaceName },

            { "ModuleId", message.Declared.ModuleId.ToString() },
            { "ModuleName", message.Declared.ModuleName },

            { "SourceRevision", message.Declared.SourceRevision },
            { "SourceUrl", message.Declared.SourceUrl },
            { "SourceRelativePath", message.Declared.SourceSubdirectory }
        };
    }
}