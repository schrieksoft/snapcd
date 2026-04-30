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