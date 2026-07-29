// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SnapCd.Contracts;
using SnapCd.Contracts.Mcp;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Documents each operation from <see cref="EndpointDocConvention"/>: the summary (overridable
/// with [EndpointSummary] on the action) and the parameter / request-body descriptions
/// (overridable with [Description] on the parameter). Entity names come from [McpEntity] when
/// present, else are derived from the controller name.
/// </summary>
public class EndpointDocOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        var entity = action.ControllerTypeInfo.GetCustomAttribute<McpEntityAttribute>(inherit: true);
        var singular = entity?.Singular ?? EndpointDocConvention.Singular(action.ControllerTypeInfo.Name);
        var plural = entity?.Plural ?? EndpointDocConvention.Plural(singular);
        var actionName = action.MethodInfo.Name;

        if (string.IsNullOrEmpty(operation.Summary))
        {
            operation.Summary = action.MethodInfo.GetCustomAttribute<EndpointSummaryAttribute>()?.Summary
                                ?? EndpointDocConvention.Summary(actionName, singular, plural);
        }

        var methodParams = action.MethodInfo.GetParameters();

        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter concrete || !string.IsNullOrEmpty(concrete.Description))
                continue;
            concrete.Description = Describe(methodParams, parameter.Name, actionName, singular);
        }

        if (operation.RequestBody is OpenApiRequestBody body && string.IsNullOrEmpty(body.Description))
        {
            var bodyParamName = context.Description.ParameterDescriptions
                .FirstOrDefault(p => p.Source == BindingSource.Body)?.ParameterDescriptor?.Name;
            if (bodyParamName is not null)
                body.Description = Describe(methodParams, bodyParamName, actionName, singular);
        }

        return Task.CompletedTask;
    }

    private static string? Describe(ParameterInfo[] methodParams, string? paramName, string actionName, string singular)
    {
        if (paramName is null) return null;

        var methodParam = methodParams.FirstOrDefault(p => p.Name == paramName);
        return methodParam?.GetCustomAttribute<DescriptionAttribute>()?.Description
               ?? EndpointDocConvention.ParamDescription(paramName, actionName, singular);
    }
}
