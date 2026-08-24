// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.Data.SqlClient;
using SnapCd.Server.Core.Services.Resilience;

namespace SnapCd.Server.Core.Tests.Tests.Resilience;

public class TransientFaultClassifierTests
{
    private readonly TransientFaultClassifier _classifier = new();

    [Fact]
    public void Deadlock_IsRetryable()
    {
        Assert.True(_classifier.IsRetryable(SqlError(1205), out var fault));
        Assert.Equal("deadlock", fault!.Reason);
    }

    [Fact]
    public void ConnectionFault_IsRetryable()
    {
        Assert.True(_classifier.IsRetryable(SqlError(10053), out _));
    }

    [Theory]
    [InlineData(2627)]
    [InlineData(2601)]
    public void UniqueViolationInTheEdgeReconcile_IsRetryable(int number)
    {
        Assert.True(_classifier.IsRetryable(SqlError(number, "trg_ModuleInputs_DependencyEdges"), out var fault));
        Assert.Equal("trg_ModuleInputs_DependencyEdges", fault!.Procedure);
    }

    [Fact]
    public void UniqueViolationInAnUnrelatedProcedure_IsNotRetryable()
    {
        // Only the reconcile is known to collide benignly; elsewhere a duplicate is a duplicate.
        Assert.False(_classifier.IsRetryable(SqlError(2627, "trg_SomeOtherTrigger"), out _));
    }

    [Theory]
    [InlineData(2627)]
    [InlineData(2601)]
    public void GenuineDuplicate_IsNotRetryable(int number)
    {
        // No procedure: the statement itself violated the constraint, so no attempt can succeed.
        Assert.False(_classifier.IsRetryable(SqlError(number), out var fault));
        Assert.Null(fault);
    }

    [Fact]
    public void UnrelatedException_IsNotRetryable()
    {
        Assert.False(_classifier.IsRetryable(new InvalidOperationException("nope"), out _));
    }

    [Fact]
    public void RetryableFaultWrappedInAnotherException_IsFound()
    {
        var wrapped = new InvalidOperationException("outer", SqlError(1205));
        Assert.True(_classifier.IsRetryable(wrapped, out _));
    }

    /// <summary>SqlException cannot be constructed directly; assemble one via its internal factory.</summary>
    private static SqlException SqlError(int number, string procedure = "")
    {
        var error = (SqlError)Activator.CreateInstance(
            typeof(SqlError),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [number, (byte)0, (byte)0, "server", "message", procedure, 0, 0, null!],
            null)!;

        var collection = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection), BindingFlags.Instance | BindingFlags.NonPublic, null, [], null)!;

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(collection, [error]);

        return (SqlException)typeof(SqlException)
            .GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
                null, [typeof(SqlErrorCollection), typeof(string)], null)!
            .Invoke(null, [collection, "6.0.0"])!;
    }
}
