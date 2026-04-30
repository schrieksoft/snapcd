using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views.Interfaces;

public interface IRecursiveDependency
{
    // Defined Module (source of edge)
    Guid DefinedModuleId { get; }
    string DefinedModuleName { get; }
    Guid DefinedNamespaceId { get; }
    string DefinedNamespaceName { get; }
    Guid DefinedStackId { get; }
    string DefinedStackName { get; }
    string DefinedDisplayName { get; }
    ActualStateHeadline? DefinedLatestActualState { get; }
    DesiredStateHeadline? DefinedDesiredState { get; }
    DesiredStateHeadline? DefinedQueuedDesiredState { get; }
    bool DefinedIsRunning { get; }
    bool DefinedIsQueued { get; }
    DesiredStateHeadline? DefinedRunningDesiredState { get; }

    // Referenced Module (target of edge)
    Guid ReferencedModuleId { get; }
    string ReferencedModuleName { get; }
    Guid ReferencedNamespaceId { get; }
    string ReferencedNamespaceName { get; }
    Guid ReferencedStackId { get; }
    string ReferencedStackName { get; }
    string ReferencedDisplayName { get; }
    ActualStateHeadline? ReferencedLatestActualState { get; }
    DesiredStateHeadline? ReferencedDesiredState { get; }
    DesiredStateHeadline? ReferencedQueuedDesiredState { get; }
    bool ReferencedIsRunning { get; }
    bool ReferencedIsQueued { get; }
    DesiredStateHeadline? ReferencedRunningDesiredState { get; }

    // Navigation properties
    Module DefinedModule { get; }
    Module ReferencedModule { get; }
    Namespace DefinedNamespace { get; }
    Namespace ReferencedNamespace { get; }
    Stack DefinedStack { get; }
    Stack ReferencedStack { get; }
}
