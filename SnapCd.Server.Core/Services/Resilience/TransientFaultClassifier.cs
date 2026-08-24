// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Data.SqlClient;

namespace SnapCd.Server.Core.Services.Resilience;

public interface ITransientFaultClassifier
{
    bool IsRetryable(Exception exception, out TransientFault? fault);
}

/// <summary>What was retried, for the log line.</summary>
public record TransientFault(int Number, string? Procedure, string Reason);

public class TransientFaultClassifier : ITransientFaultClassifier
{
    private const int Deadlock = 1205;
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    // The reconcile path whose unique violations are a lost race rather than a real duplicate:
    // the edge row it collided with is committed by the next attempt. Anything not named here
    // is treated as a genuine duplicate, because retrying one can never succeed.
    private static readonly HashSet<string> RaceProneProcedures = new(StringComparer.OrdinalIgnoreCase)
    {
        "sp_UpdateDependencyEdgesForModules",
        "trg_ModuleInputs_DependencyEdges",
        "trg_DependsOnModules_DependencyEdges"
    };

    // Connection faults, failovers and throttling, per SQL Server's documented transient set.
    private static readonly HashSet<int> ConnectionAndThrottling =
    [
        49918, 49919, 49920, 40613, 40501, 40197, 40143, 233, 121, 64, 20, 10928, 10929,
        10053, 10054, 10060, 4060, 4221, 1221, -2
    ];

    public bool IsRetryable(Exception exception, out TransientFault? fault)
    {
        fault = null;

        foreach (var error in Unwrap(exception).SelectMany(e => e.Errors.Cast<SqlError>()))
        {
            if (error.Number == Deadlock)
            {
                fault = new TransientFault(error.Number, NullIfEmpty(error.Procedure), "deadlock");
                return true;
            }

            if (ConnectionAndThrottling.Contains(error.Number))
            {
                fault = new TransientFault(error.Number, NullIfEmpty(error.Procedure), "connection or throttling fault");
                return true;
            }

            if (error.Number is UniqueConstraintViolation or UniqueIndexViolation
                && RaceProneProcedures.Contains(error.Procedure ?? ""))
            {
                fault = new TransientFault(error.Number, error.Procedure, "unique violation in the dependency-edge reconcile");
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<SqlException> Unwrap(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
            if (e is SqlException sql)
                yield return sql;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
