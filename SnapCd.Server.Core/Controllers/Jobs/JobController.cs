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
using SnapCd.Server.Core.Misc.Exceptions;
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

    /// <summary>Start an Apply job for a Module. Returns the Job ID. If correlationId is provided, that ID is used; otherwise a new GUID is generated.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Module ID to apply</param>
    /// <param name="correlationId">Optional correlation ID to use as Job ID</param>
    [HttpPost("apply/{id}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<Guid>> Apply(Guid organizationId, Guid id, [FromQuery] Guid? correlationId)
    {
        try
        {
            return await Service.Apply(id, organizationId, correlationId);
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

    /// <summary>Start a Destroy job for a Module. Returns the Job ID. If correlationId is provided, that ID is used; otherwise a new GUID is generated.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Module ID to destroy</param>
    /// <param name="correlationId">Optional correlation ID to use as Job ID</param>
    [HttpPost("destroy/{id}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<Guid>> Destroy(Guid organizationId, Guid id, [FromQuery] Guid? correlationId)
    {
        try
        {
            return await Service.Destroy(id, organizationId, correlationId);
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

    /// <summary>Record an approval decision on a Job pending approval. The calling principal (the Agent) is recorded as the approver.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Job ID to approve</param>
    /// <param name="dto">Optional approval payload (reason)</param>
    [HttpPost("{id}/approve")]
    [ExposeAsMcpTool]
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

    /// <summary>Record a decline decision on a Job pending approval. The calling principal (the Agent) is recorded as the decliner. A reason is required.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Job ID to decline</param>
    /// <param name="dto">Decline payload (reason required)</param>
    [HttpPost("{id}/decline")]
    [ExposeAsMcpTool]
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

    /// <summary>Redacted log entries of a SnapCd ModuleJob, as a JSON array of LogEntryDto. Partition by TaskName (Init / Validate / Variables / Plan / ApplyFromPlan / DestroyFromPlan etc.) to focus on a single phase. Secrets and likely-credential patterns are replaced with [REDACTED:type] markers.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="jobId">Job ID</param>
    [HttpGet("{jobId}/logs")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/jobs/{jobId}/logs",
        Name = "module_job_logs")]
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
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>Status metadata for a SnapCd ModuleJob: JobType (Apply/Destroy), WaitingForApproval, ActualStateHeadline, server-side error fields, and output deltas (OutputsCreate/Modify/Destroy/Recreate/Unchanged lists). Does NOT contain the resource-action plan body or the apply output — those are in module_job_logs filtered by TaskName.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="jobId">Job ID</param>
    [HttpGet("{jobId}/status")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/jobs/{jobId}/status",
        Name = "module_job_status")]
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
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>Approval history for a SnapCd ModuleJob: who decided, when, and whether they declined. Returns an empty array if no decisions have been recorded (i.e. the job either auto-applied or is still awaiting approval). The principal-resolution details (name/email) are not returned — only the principal id and discriminator (User / ServicePrincipal).</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="jobId">Job ID</param>
    [HttpGet("{jobId}/approvals")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/jobs/{jobId}/approvals",
        Name = "module_job_approvals")]
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
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>Cancel a running Job. CancellationType: AfterCurrent (let current step finish), ImmediateGraceful (signal runner to stop), ImmediateKill (force terminate).</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Job ID to cancel</param>
    /// <param name="cancellationType">Cancellation strategy</param>
    [HttpPost("{id}/cancel")]
    [ExposeAsMcpTool]
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
