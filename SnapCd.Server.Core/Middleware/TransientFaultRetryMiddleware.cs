// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Services.Resilience;

namespace SnapCd.Server.Core.Middleware;

/// <summary>
/// Retries a write request whose database fault is transient — a deadlock, a connection fault, or a
/// unique violation raised by the dependency-graph triggers rather than by the statement itself.
/// </summary>
public class TransientFaultRetryMiddleware
{
    private const int MaxAttempts = 3;
    private static readonly int[] BackoffMilliseconds = [20, 60];

    private readonly RequestDelegate _next;

    public TransientFaultRetryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IServiceScopeFactory scopeFactory,
        ITransientFaultClassifier classifier,
        ILogger<TransientFaultRetryMiddleware> logger)
    {
        if (!IsWrite(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        var originalServices = context.RequestServices;

        try
        {
            for (var attempt = 1; ; attempt++)
            {
                // Buffer the response so a failed attempt writes nothing to the client, and give
                // each attempt its own scope: the DbContext is scoped, and replaying a request
                // against the change tracker of a failed attempt would write duplicates.
                using var buffer = new MemoryStream();
                using var scope = scopeFactory.CreateScope();
                context.Response.Body = buffer;
                context.RequestServices = scope.ServiceProvider;

                try
                {
                    await _next(context);
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody);
                    return;
                }
                catch (Exception e) when (attempt < MaxAttempts && classifier.IsRetryable(e, out var fault))
                {
                    logger.LogWarning(
                        "Retrying {Method} {Path} after transient database fault ({Reason}, error {Number}{Procedure}); attempt {Attempt} of {MaxAttempts}.",
                        context.Request.Method, context.Request.Path, fault!.Reason, fault.Number,
                        fault.Procedure is null ? "" : $" in {fault.Procedure}", attempt, MaxAttempts);

                    context.Response.Clear();
                    await Task.Delay(BackoffMilliseconds[attempt - 1], context.RequestAborted);
                }
            }
        }
        finally
        {
            context.Response.Body = originalBody;
            context.RequestServices = originalServices;
        }
    }

    private static bool IsWrite(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}
