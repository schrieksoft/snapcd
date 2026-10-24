// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.StateMachine.SplitMigrate;
using SnapCd.Server.Core.StateMachine.StateMigrations;
using SnapCd.Server.Core.StateMachine.Transfers.Migrate;
using Xunit;

using ApplyMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.ApplyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.ApplyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionRequested,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyGetDefinitiveRevisionFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateRequested,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyPolicyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyOutputRequested,
    SnapCd.Server.Core.Events.Steps.ApplyOutputCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyOutputCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyOutputFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleRequested,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyGetModuleFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyInitRequested,
    SnapCd.Server.Core.Events.Steps.ApplyInitCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyInitCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyInitFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyValidateRequested,
    SnapCd.Server.Core.Events.Steps.ApplyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesRequested,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyVariablesFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyPlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyPlanFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanCancelled,
    SnapCd.Server.Core.Events.Steps.ApplyFromPlanFaulted,
    SnapCd.Server.Core.Events.Steps.ApplyCancelKillRequested,
    SnapCd.Server.Core.Events.Steps.DummyApplyCancelKillCompleted,
    SnapCd.Server.Core.Events.Steps.ApplyCancelKillCompleted>;
using DestroyMachine = SnapCd.Server.Core.StateMachine.Jobs.JobStateMachine<
    SnapCd.Server.Core.Entities.Sagas.DestroyJobSaga,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleRequested,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleFailed,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleCompleted,
    SnapCd.Server.Core.Events.Jobs.Module.DestroyModuleCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionRequested,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyGetDefinitiveRevisionFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateRequested,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyPolicyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyOutputRequested,
    SnapCd.Server.Core.Events.Steps.DestroyOutputCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyOutputCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyOutputFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleRequested,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyGetModuleFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyInitRequested,
    SnapCd.Server.Core.Events.Steps.DestroyInitCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyInitCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyInitFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyValidateRequested,
    SnapCd.Server.Core.Events.Steps.DestroyValidateCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyValidateCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyValidateFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesRequested,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyVariablesFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyPlanRequested,
    SnapCd.Server.Core.Events.Steps.DestroyPlanCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyPlanCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyPlanFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanRequested,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanCancelled,
    SnapCd.Server.Core.Events.Steps.DestroyFromPlanFaulted,
    SnapCd.Server.Core.Events.Steps.DestroyCancelKillRequested,
    SnapCd.Server.Core.Events.Steps.DummyDestroyCancelKillCompleted,
    SnapCd.Server.Core.Events.Steps.DestroyCancelKillCompleted>;

namespace SnapCd.Server.Core.Tests.Tests.StateMachine;

/// <summary>
/// Structural rules that keep a step's failure from going unnoticed: every fault type is listened for, every step result's
/// handling can fail the job, and each step request has a single consumer, so a fault of it can only come from that step.
/// </summary>
public class StepFaultConventionTests
{
    private static readonly Type[] MachineTypes =
    [
        typeof(ApplyMachine), typeof(DestroyMachine),
        typeof(MoveStateMachine), typeof(ImportStateMachine), typeof(RemoveStateMachine),
        typeof(LookupAddressesStateMachine), typeof(SplitMigrateStateMachine), typeof(TransferMigrateStateMachine)
    ];

    private static readonly Assembly Core = typeof(StepFaultedBase).Assembly;

    private static Dictionary<Type, HashSet<Type>> EventTypesByMachine()
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var result = new Dictionary<Type, HashSet<Type>>();
        foreach (var machineType in MachineTypes)
        {
            var machine = (MassTransit.StateMachine)ActivatorUtilities.CreateInstance(services, machineType);
            result[machineType] = machine.Events
                .Select(e => e.GetType().GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Event<>))?
                    .GetGenericArguments()[0])
                .Where(t => t != null)
                .Select(t => t!)
                .ToHashSet();
        }

        return result;
    }

    private static bool IsStepResponse(Type type) =>
        typeof(StepResponseBase).IsAssignableFrom(type) || typeof(StateMigrationStepResponseBase).IsAssignableFrom(type);

    [Fact]
    public void Every_Fault_Type_Is_An_Event_Of_Some_State_Machine()
    {
        var listenedFor = EventTypesByMachine().Values.SelectMany(x => x).ToHashSet();

        var unheard = Core.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IStepFault).IsAssignableFrom(t))
            .Where(t => !listenedFor.Contains(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(unheard.Count == 0, "Fault types no state machine listens for:\n" + string.Join("\n", unheard));
    }

    [Fact]
    public void Every_Step_Result_A_Machine_Handles_Has_A_Fault_Event_In_That_Machine()
    {
        var missing = new List<string>();
        foreach (var (machine, events) in EventTypesByMachine())
        foreach (var completed in events.Where(t => t.Name.EndsWith("Completed") && IsStepResponse(t) && !t.Name.Contains("CancelKill") && !t.Name.StartsWith("Heartbeat")))
            if (!events.Contains(typeof(Fault<>).MakeGenericType(completed)))
                missing.Add($"{machine.Name}: Fault<{completed.Name}>");

        Assert.True(missing.Count == 0, "Step results whose handling cannot fail the job:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void Every_Step_Request_Has_Exactly_One_Consumer()
    {
        var requestBases = new[] { typeof(StepRequestBase), typeof(StateMigrationStepRequestBase) };

        var consumersByMessage = Core.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => (consumer: t, message: i.GetGenericArguments()[0])))
            .Where(x => x.message.Name.EndsWith("Requested") && requestBases.Any(b => b.IsAssignableFrom(x.message)))
            .GroupBy(x => x.message)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Name}: {string.Join(", ", g.Select(x => x.consumer.Name))}")
            .ToList();

        Assert.True(consumersByMessage.Count == 0, "Step requests with more than one consumer:\n" + string.Join("\n", consumersByMessage));
    }
}
