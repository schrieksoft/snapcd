// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Jobs;
using SnapCd.Contracts.Dto.ModuleJobApprovals;
using SnapCd.Contracts.Mcp;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;

namespace SnapCd.Server.Core.Controllers.Jobs;

[Route(ControllerEndpoints.Jobs)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
[McpEntity(Singular = "Job", Plural = "Jobs")]
public class JobController : ControllerBase
{
    protected readonly JobOrchestrationService Service;

    public JobController(JobOrchestrationService service)
    {
        Service = service;
    }

    [EndpointSummary("Start an Apply job for a Module")]
    [PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.RunJob)]
    [HttpPost("apply/{moduleId}")]
    [ExposeAsMcpTool(Instructions = "Returns the Job ID. If correlationId is provided, that ID is used; otherwise a new GUID is generated.")]
    public async Task<ActionResult<Guid>> Apply(Guid organizationId, Guid moduleId, [FromQuery] Guid? correlationId)
    {
        try
        {
            return await Service.Apply(moduleId, organizationId, correlationId);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Start a Destroy job for a Module")]
    [PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.RunJob)]
    [HttpPost("destroy/{moduleId}")]
    [ExposeAsMcpTool(Instructions = "Returns the Job ID. If correlationId is provided, that ID is used; otherwise a new GUID is generated.")]
    public async Task<ActionResult<Guid>> Destroy(Guid organizationId, Guid moduleId, [FromQuery] Guid? correlationId)
    {
        try
        {
            return await Service.Destroy(moduleId, organizationId, correlationId);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Record an approval decision on a Job")]
    [PermissionSource(Repository = typeof(ModuleJobApprovalSecuredRepository), Verb = PermissionVerb.Create)]
    [HttpPost("{id}/approve")]
    [ExposeAsMcpTool(Instructions = "The Job must be pending approval. The calling principal is recorded as the approver.")]
    public async Task<IActionResult> Approve(Guid organizationId, Guid id, [FromBody] ApproveJobDto? dto = null)
    {
        try
        {
            await Service.Approve(id, organizationId, dto?.Reason);
            return Ok($"Job '{id}' approved");
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Record a decline decision on a Job")]
    [PermissionSource(Repository = typeof(ModuleJobApprovalSecuredRepository), Verb = PermissionVerb.Create)]
    [HttpPost("{id}/decline")]
    [ExposeAsMcpTool(Instructions = "The Job must be pending approval. The calling principal is recorded as the decliner. A reason is required.")]
    public async Task<IActionResult> Decline(Guid organizationId, Guid id, [FromBody] DeclineJobDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest("A reason is required when declining a job approval.");

        try
        {
            await Service.Decline(id, organizationId, dto.Reason);
            return Ok($"Job '{id}' declined");
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Get redacted log entries of a Job")]
    [PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.Read)]
    [HttpGet("{jobId}/logs")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/jobs/{jobId}/logs",
        Name = "module_job_logs",
        Instructions = "Returned as a JSON array of LogEntryDto. Partition by TaskName (Init / Validate / Variables / Plan / ApplyFromPlan / DestroyFromPlan etc.) to focus on a single phase. Secrets and likely-credential patterns are replaced with [REDACTED:type] markers.")]
    public async Task<ActionResult<string>> GetLogs(Guid organizationId, Guid jobId)
    {
        try
        {
            return await Service.GetLogs(jobId, organizationId);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Get status metadata for a Job")]
    [PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.Read)]
    [HttpGet("{jobId}/status")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/jobs/{jobId}/status",
        Name = "module_job_status",
        Instructions = "Covers JobType (Apply/Destroy), DefinitiveRevision (the resolved commit SHA the job ran against), WaitingForApproval, ActualStateHeadline, server-side error fields, and output deltas (OutputsCreate/Modify/Destroy/Recreate/Unchanged lists). Does NOT contain the resource-action plan body or the apply output — those are in module_job_logs filtered by TaskName.")]
    public async Task<ActionResult<ModuleJobStatusDto>> GetStatus(Guid organizationId, Guid jobId)
    {
        try
        {
            return await Service.GetStatus(jobId, organizationId);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Get approval history for a Job")]
    [PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.Read)]
    [HttpGet("{jobId}/approvals")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/jobs/{jobId}/approvals",
        Name = "module_job_approvals",
        Instructions = "Shows who decided, when, and whether they declined. Returns an empty array if no decisions have been recorded (i.e. the job either auto-applied or is still awaiting approval). The principal-resolution details (name/email) are not returned — only the principal id and discriminator (User / ServicePrincipal).")]
    public async Task<ActionResult<List<ModuleJobApprovalReadDto>>> GetApprovals(Guid organizationId, Guid jobId)
    {
        try
        {
            return await Service.GetApprovals(jobId, organizationId);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Cancel a running Job")]
    [PermissionSource(Repository = typeof(ModuleJobSecuredRepository), Verb = PermissionVerb.RunJob)]
    [HttpPost("{id}/cancel")]
    [ExposeAsMcpTool(Instructions = "CancellationType: AfterCurrent (let current step finish), ImmediateGraceful (signal runner to stop), ImmediateKill (force terminate).")]
    public async Task<IActionResult> Cancel(
        Guid organizationId,
        Guid id,
        [FromQuery] CancellationType cancellationType = CancellationType.ImmediateGraceful)
    {
        try
        {
            await Service.Cancel(id, organizationId, cancellationType);
            return Ok($"Job '{id}' cancelled ({cancellationType})");
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}
