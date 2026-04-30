using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Records user acceptance of Terms of Service for audit purposes.
/// This is an immutable record - once created, it should not be modified.
/// </summary>
public class TermsAcceptance
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Version of the terms that were accepted (format: YYYY-MM-DD).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string TermsVersion { get; set; }

    /// <summary>
    /// Context in which the terms were accepted.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string AcceptanceContext { get; set; }

    /// <summary>
    /// UTC timestamp when the user accepted the terms.
    /// </summary>
    public DateTime AcceptedDateTime { get; set; }

    /// <summary>
    /// IP address of the user at time of acceptance (for audit).
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the browser at time of acceptance (for audit).
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Constants for AcceptanceContext values.
/// </summary>
public static class TermsAcceptanceContext
{
    public const string Registration = "Registration";
    public const string InvitationRegistration = "InvitationRegistration";
    public const string TermsUpdate = "TermsUpdate";
}
