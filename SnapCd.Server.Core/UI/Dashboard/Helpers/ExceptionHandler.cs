// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using EntityFramework.Exceptions.Common;
using MudBlazor;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.UI.Dashboard.Helpers;

public static class ExceptionHandler
{
    public static void HandleException(ISnackbar snackbar, Exception ex, string operation)
    {
        var message = ex switch
        {
            InvalidStackReferenceException => "Invalid reference: The selected item belongs to a different stack.",
            InvalidSecretScopeException => "Invalid secret scope: The secret cannot be used in this context.",
            UniqueConstraintException => "A record with this name or identifier already exists.",
            EntityNotFoundException => "The requested item was not found. It may have been deleted.",
            IdIsEmptyException => "Invalid operation: Entity ID is missing.",
            OrganizationIdIsEmptyException => "Invalid operation: Organization ID is missing.",
            QuotaExceededException qe => $"Quota exceeded: {qe.EntityType} limit of {qe.Limit} reached.",
            LicenseLimitExceededException le => $"License limit exceeded: {le.ResourceType} limit of {le.Limit} reached.",
            PrincipalNotAuthorizedException => "You do not have permission to perform this action.",
            _ => $"Error {operation}: {ex.Message}"
        };

        snackbar.Add(message, Severity.Error);
    }
}
