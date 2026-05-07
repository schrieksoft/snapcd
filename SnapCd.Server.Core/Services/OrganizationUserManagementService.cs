using System.Security.Cryptography;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Exceptions;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.Email;
using SnapCd.Server.Core.Services.QuotaUsage;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public class MemberService : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly OrganizationUserRepository _orgUserRepo;
    private readonly UserManager<User> _userManager;
    private readonly IQuotaUsageForInvitationService _rateLimitService;
    private readonly IOptions<InvitationSettings> _settings;
    private readonly ISnapCdEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBus _bus;
    private readonly ILogger<MemberService> _logger;

    public MemberService(
        SnapCdDbContext dbContext,
        OrganizationUserRepository orgUserRepo,
        UserManager<User> userManager,
        IQuotaUsageForInvitationService rateLimitService,
        IOptions<InvitationSettings> settings,
        ISnapCdEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor,
        IBus bus,
        ILogger<MemberService> logger)
    {
        _dbContext = dbContext;
        _orgUserRepo = orgUserRepo;
        _userManager = userManager;
        _rateLimitService = rateLimitService;
        _settings = settings;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
        _bus = bus;
        _logger = logger;
    }

    public async Task<IEnumerable<OrganizationUser>> GetMembersAsync(Guid organizationId)
    {
        return await _orgUserRepo.ListByOrganizationId(organizationId);
    }

    public async Task<OrganizationUser?> GetMemberAsync(Guid organizationId, Guid userId)
    {
        return await _orgUserRepo.Get(organizationId, userId);
    }

    public async Task<(OrganizationUser OrgUser, string InvitationLink, bool EmailSent)> InviteMemberAsync(Guid organizationId, string email, Guid invitingUserId, int? expirationDays = null)
    {
        // Get expiration days from settings if not provided
        var expirationDaysToUse = expirationDays ?? _settings.Value.ExpirationDays;

        // Check if organization exists
        var organization = await _dbContext.Organizations.FindAsync(organizationId);
        if (organization == null)
            throw new ArgumentException("Organization not found", nameof(organizationId));

        // Check if inviting user's email is verified (if setting is enabled)
        if (_settings.Value.RequireEmailVerification)
        {
            var invitingUser = await _userManager.FindByIdAsync(invitingUserId.ToString());
            if (invitingUser == null || !invitingUser.EmailConfirmed)
            {
                throw new EmailVerificationRequiredException();
            }
        }

        // Check rate limits BEFORE creating user or doing anything else
        await _rateLimitService.CheckAndRecordInvitationAsync(invitingUserId, organizationId, email);

        // Check if user with this email already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        User user;

        if (existingUser != null)
        {
            user = existingUser;
        }
        else
        {
            // Create new user for the invitation
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false, // Will be confirmed when they complete invitation
                IsRegistrationNotCompleted = true, // User must complete registration on following invitation link, or alternatively register in the normal way
                InvitationCreatedDateTime = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            await _bus.Publish(new UserCreatedEvent
            {
                Data = UserMapper.ToDto(user),
                CreatedDateTime = DateTime.UtcNow
            });
        }

        // Generate token for the invitation
        var token = GenerateSecureToken();
        var expirationDateTime = DateTime.UtcNow.AddDays(expirationDaysToUse);

        // Set invitation fields on the user (for new users or users without completed invitations)
        if (existingUser == null) await _userManager.UpdateAsync(user);

        // Create or update the organization OrganizationUser
        var organizationUser = await _orgUserRepo.Get(organizationId, user.Id);

        if (organizationUser != null)
        {
            // Update existing OrganizationUser with new invitation
            organizationUser.IsDeactivated = true; // Keep deactivated until accepted
            organizationUser.InvitationToken = token;
            organizationUser.InvitationSentDateTime = DateTime.UtcNow;
            organizationUser.InvitationExpirationDateTime = expirationDateTime;
            organizationUser.InvitationCompleted = false;
            await _orgUserRepo.Update(organizationUser);
        }
        else
        {
            // Create new OrganizationUser
            organizationUser = new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = user.Id,
                IsDeactivated = true, // Will be activated when invitation is completed
                InvitationToken = token,
                InvitationSentDateTime = DateTime.UtcNow,
                InvitationExpirationDateTime = expirationDateTime,
                InvitationCompleted = false
            };
            organizationUser = await _orgUserRepo.Create(organizationUser);
        }
        
        var organizationName = _dbContext.Organizations.Where(x => x.Id  == organizationId).Select( x => x.Name).Single();
        
        // Build invitation link and send email
        var inviter = await _userManager.FindByIdAsync(invitingUserId.ToString());
        var inviterName = inviter != null
            ? $"{inviter.FirstName} {inviter.LastName}".Trim()
            : "Unknown";
        if (string.IsNullOrWhiteSpace(inviterName))
            inviterName = inviter?.Email ?? "Unknown";
        var inviterEmail = inviter?.Email ?? "Unknown";

        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request != null
            ? $"{request.Scheme}://{request.Host}"
            : "https://localhost";
        var invitationLink = $"{baseUrl}/Account/AcceptInvitation?token={token}";

        var emailSent = await _emailSender.SendOrganizationInvitationAsync(
            email,
            organizationName,
            inviterName,
            inviterEmail,
            invitationLink,
            expirationDaysToUse);

        _logger.LogInformation(
            "Invitation created for {Email} to organization {OrgId} by user {InviterId} (email delivered: {EmailSent})",
            email, organizationId, invitingUserId, emailSent);

        return (organizationUser, invitationLink, emailSent);
    }

    public async Task<OrganizationUser?> GetInvitationByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        // Find the OrganizationUser by invitation token
        return await _orgUserRepo.GetByInvitationTokenIncludingAll(token);
    }

    public async Task<string?> GetPendingInvitationTokenForUserAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.OrganizationUsers
            .Where(ou => ou.UserId == userId
                         && !ou.InvitationCompleted
                         && ou.InvitationToken != null
                         && (ou.InvitationExpirationDateTime == null || ou.InvitationExpirationDateTime > now))
            .OrderByDescending(ou => ou.InvitationSentDateTime)
            .Select(ou => ou.InvitationToken)
            .FirstOrDefaultAsync();
    }

    public async Task<OrganizationUser> CompleteInvitationAsync(string token)
    {
        // Find OrganizationUser by invitation token
        var organizationUser = await _orgUserRepo.GetByInvitationTokenIncludingAll(token);

        if (organizationUser == null)
            throw new InvalidOperationException("Invalid invitation token");

        if (organizationUser.InvitationCompleted)
            throw new InvalidOperationException("Invitation has already been completed");

        if (organizationUser.InvitationExpirationDateTime <= DateTime.UtcNow)
            throw new InvitationExpiredException(organizationUser.InvitationExpirationDateTime ?? DateTime.MinValue);

        // Complete the invitation on both user and OrganizationUser
        var completedDateTime = DateTime.UtcNow;


        // Update OrganizationUser
        organizationUser.InvitationCompleted = true;
        organizationUser.InvitationCompletedDateTime = completedDateTime;
        organizationUser.InvitationToken = null; // Clear token after use
        organizationUser.IsDeactivated = false; // Activate the OrganizationUser

        await _orgUserRepo.Update(organizationUser);

        _logger.LogInformation("Invitation completed for {Email} to organization {OrgId}",
            organizationUser.User.Email, organizationUser.OrganizationId);

        return organizationUser;
    }

    public async Task CancelInvitationAsync(Guid organizationUserId, Guid organizationId)
    {
        // Query directly: the repo's Get(orgId, userId) overload takes a UserId (not an
        // OrganizationUser.Id) AND filters by !IsDeactivated. Pending invitations are
        // intentionally created with IsDeactivated=true (see InviteMemberAsync), so they're
        // invisible to that path. Look them up by their primary key + OrganizationId scope.
        var organizationUser = await _dbContext.OrganizationUsers
            .Include(ou => ou.User)
            .FirstOrDefaultAsync(ou => ou.Id == organizationUserId
                                       && ou.OrganizationId == organizationId);

        if (organizationUser == null)
            throw new InvalidOperationException("OrganizationUser not found");

        if (organizationUser.InvitationCompleted)
            throw new InvalidOperationException("Cannot cancel a completed invitation");

        if (organizationUser.User == null)
            throw new InvalidOperationException("No user associated with this OrganizationUser");

        // Remove the OrganizationUser row entirely — there is no "cancelled" state to keep.
        await _orgUserRepo.Delete(organizationUser);
    }

    public async Task<OrganizationUser> DeclineInvitationAsync(string token)
    {
        // Find OrganizationUser by invitation token
        var organizationUser = await _orgUserRepo.GetByInvitationTokenIncludingAll(token);

        if (organizationUser == null)
            throw new InvalidOperationException("Invalid invitation token");

        if (organizationUser.InvitationCompleted)
            throw new InvalidOperationException("Cannot decline a completed invitation");

        // Delete the OrganizationUser record
        await _orgUserRepo.Delete(organizationUser);

        _logger.LogInformation(
            "Invitation declined for {Email} to organization {OrgId}",
            organizationUser.User.Email, organizationUser.OrganizationId);

        return organizationUser;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public async Task RemoveMemberAsync(Guid organizationUserId)
    {
        var deactivated = await _orgUserRepo.Deactivate(organizationUserId);
        if (!deactivated)
            throw new InvalidOperationException("OrganizationUser not found");
    }


    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}