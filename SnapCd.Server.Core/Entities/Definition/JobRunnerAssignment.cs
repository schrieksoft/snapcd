using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Represents the assignment of a job to a specific runner.
/// Once a runner is assigned a job, it must execute all tasks in that job,
/// even if it disconnects and reconnects.
/// </summary>
public class JobRunnerAssignment : AuditBase, IEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this assignment belongs to
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The job that is assigned to the runner
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The stable runner identity assigned to execute this job
    /// </summary>
    public Guid RunnerIdentityId { get; set; }

    /// <summary>
    /// The current task being executed (null if job not started or between tasks)
    /// </summary>
    [MaxLength(255)]
    public string? CurrentTaskId { get; set; }

    /// <summary>
    /// Number of tasks completed in this job
    /// </summary>
    public int TasksCompleted { get; set; }

    /// <summary>
    /// Total number of tasks in this job
    /// </summary>
    public int TasksTotal { get; set; }

    /// <summary>
    /// When the job was assigned to the runner
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// When the runner started working on the job
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job was completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Status of the job assignment
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "ASSIGNED"; // ASSIGNED, IN_PROGRESS, COMPLETED, FAILED, CANCELLED

    /// <summary>
    /// Error message if job failed
    /// </summary>
    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual ModuleJob Job { get; set; } = null!;

    public Guid ParentId()
    {
        return JobId;
    }
}