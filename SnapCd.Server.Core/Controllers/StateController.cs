// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Controllers;

[Route("api/state/{stateStoreId}/{stateFileName}")]
[ApiController]
[AllowAnonymous]
public class StateController : ControllerBase
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly StateFileServiceFactory _serviceFactory;

    public StateController(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IOpenIddictApplicationManager applicationManager,
        StateFileServiceFactory serviceFactory)
    {
        _dbContextFactory = dbContextFactory;
        _applicationManager = applicationManager;
        _serviceFactory = serviceFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetState(Guid stateStoreId, string stateFileName)
    {
        var authResult = await Authenticate(stateStoreId);
        if (authResult.ActionResult != null) return authResult.ActionResult;

        using var service = authResult.Service!;

        try
        {
            var stateFile = await service.FindByName(stateFileName, stateStoreId);
            if (stateFile == null)
                return NotFound();

            var latestVersion = await service.GetLatestVersion(stateFile.Id);
            if (latestVersion == null || latestVersion.Data == null)
                return NotFound();

            var decrypted = service.DecryptData(latestVersion.Data);
            return Content(decrypted!, "application/json");
        }
        catch (PrincipalNotAuthorizedException)
        {
            return StatusCode(403, new { error = "Insufficient permissions." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostState(Guid stateStoreId, string stateFileName)
    {
        var authResult = await Authenticate(stateStoreId);
        if (authResult.ActionResult != null) return authResult.ActionResult;

        using var service = authResult.Service!;

        try
        {
            var stateFile = await service.FindByName(stateFileName, stateStoreId);
            var lockId = Request.Query["ID"].ToString();

            if (stateFile != null && !string.IsNullOrEmpty(stateFile.LockId))
            {
                if (!service.IsLockExpired(stateFile) && stateFile.LockId != lockId)
                    return StatusCode(423, new { error = "State is locked by another process." });
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var encrypted = service.EncryptData(body);

            if (stateFile == null)
            {
                stateFile = new StateFile
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = authResult.OrganizationId,
                    StateStoreId = stateStoreId,
                    Name = stateFileName,
                };
                stateFile = await service.CreateStateFile(stateFile);
            }

            await service.CreateVersion(stateFile.Id, authResult.OrganizationId, encrypted, authResult.ServicePrincipalId, AuditPrincipalDiscriminator.ServicePrincipal);
            return Ok();
        }
        catch (PrincipalNotAuthorizedException)
        {
            return StatusCode(403, new { error = "Insufficient permissions." });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteState(Guid stateStoreId, string stateFileName)
    {
        var authResult = await Authenticate(stateStoreId);
        if (authResult.ActionResult != null) return authResult.ActionResult;

        using var service = authResult.Service!;

        try
        {
            var stateFile = await service.FindByName(stateFileName, stateStoreId);
            if (stateFile == null)
                return NotFound();

            await service.DeleteStateFile(stateFile.Id, authResult.OrganizationId);
            return Ok();
        }
        catch (PrincipalNotAuthorizedException)
        {
            return StatusCode(403, new { error = "Insufficient permissions." });
        }
    }

    [HttpPost("lock")]
    public async Task<IActionResult> Lock(Guid stateStoreId, string stateFileName)
    {
        var authResult = await Authenticate(stateStoreId);
        if (authResult.ActionResult != null) return authResult.ActionResult;

        using var service = authResult.Service!;

        try
        {
            using var reader = new StreamReader(Request.Body);
            var lockInfoJson = await reader.ReadToEndAsync();

            var stateFile = await service.FindByName(stateFileName, stateStoreId);

            if (stateFile == null)
            {
                var lockInfo = System.Text.Json.JsonSerializer.Deserialize<TerraformLockInfo>(lockInfoJson);
                stateFile = new StateFile
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = authResult.OrganizationId,
                    StateStoreId = stateStoreId,
                    Name = stateFileName,
                    LockId = lockInfo?.ID,
                    LockInfo = lockInfoJson,
                    LockCreatedAt = DateTimeOffset.UtcNow,
                    LockedById = authResult.ServicePrincipalId,
                    LockedByPrincipalDiscriminator = AuditPrincipalDiscriminator.ServicePrincipal
                };
                await service.CreateStateFile(stateFile);
                return Ok();
            }

            if (!string.IsNullOrEmpty(stateFile.LockId) && !service.IsLockExpired(stateFile))
            {
                return new ContentResult { StatusCode = 423, Content = stateFile.LockInfo, ContentType = "application/json" };
            }

            var info = System.Text.Json.JsonSerializer.Deserialize<TerraformLockInfo>(lockInfoJson);
            stateFile.LockId = info?.ID;
            stateFile.LockInfo = lockInfoJson;
            stateFile.LockCreatedAt = DateTimeOffset.UtcNow;
            stateFile.LockedById = authResult.ServicePrincipalId;
            stateFile.LockedByPrincipalDiscriminator = AuditPrincipalDiscriminator.ServicePrincipal;
            await service.UpdateStateFile(stateFile);
            return Ok();
        }
        catch (PrincipalNotAuthorizedException)
        {
            return StatusCode(403, new { error = "Insufficient permissions." });
        }
    }

    [HttpPost("unlock")]
    public async Task<IActionResult> Unlock(Guid stateStoreId, string stateFileName)
    {
        var authResult = await Authenticate(stateStoreId);
        if (authResult.ActionResult != null) return authResult.ActionResult;

        using var service = authResult.Service!;

        try
        {
            using var reader = new StreamReader(Request.Body);
            var unlockInfoJson = await reader.ReadToEndAsync();
            var unlockInfo = System.Text.Json.JsonSerializer.Deserialize<TerraformLockInfo>(unlockInfoJson);

            var stateFile = await service.FindByName(stateFileName, stateStoreId);
            if (stateFile == null)
                return NotFound();

            if (stateFile.LockId != unlockInfo?.ID)
                return StatusCode(423, new { error = "Lock ID does not match." });

            stateFile.LockId = null;
            stateFile.LockInfo = null;
            stateFile.LockCreatedAt = null;
            stateFile.LockedById = null;
            stateFile.LockedByPrincipalDiscriminator = null;
            await service.UpdateStateFile(stateFile);
            return Ok();
        }
        catch (PrincipalNotAuthorizedException)
        {
            return StatusCode(403, new { error = "Insufficient permissions." });
        }
    }

    [HttpPost("force-unlock")]
    public async Task<IActionResult> ForceUnlock(Guid stateStoreId, string stateFileName)
    {
        var authResult = await Authenticate(stateStoreId);
        if (authResult.ActionResult != null) return authResult.ActionResult;

        using var service = authResult.Service!;

        try
        {
            var stateFile = await service.GetByName(stateFileName, stateStoreId, authResult.OrganizationId);

            stateFile.LockId = null;
            stateFile.LockInfo = null;
            stateFile.LockCreatedAt = null;
            stateFile.LockedById = null;
            stateFile.LockedByPrincipalDiscriminator = null;
            await service.UpdateStateFile(stateFile);
            return Ok();
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
        catch (PrincipalNotAuthorizedException)
        {
            return StatusCode(403, new { error = "Insufficient permissions." });
        }
    }

    private async Task<AuthResult> Authenticate(Guid stateStoreId)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthResult.Fail(Unauthorized(new { error = "Basic authentication required." }));

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader["Basic ".Length..]));
        }
        catch
        {
            return AuthResult.Fail(Unauthorized(new { error = "Invalid Basic auth encoding." }));
        }

        var colonIndex = credentials.LastIndexOf(':');
        if (colonIndex < 0)
            return AuthResult.Fail(Unauthorized(new { error = "Invalid credentials format." }));

        var username = credentials[..colonIndex];
        var password = credentials[(colonIndex + 1)..];

        // Username format: "{organizationId}:{clientId}"
        var usernameColonIndex = username.IndexOf(':');
        if (usernameColonIndex < 0)
            return AuthResult.Fail(Unauthorized(new { error = "Username must be in format 'organizationId:clientId'." }));

        if (!Guid.TryParse(username[..usernameColonIndex], out var organizationId))
            return AuthResult.Fail(Unauthorized(new { error = "Invalid organizationId in username." }));

        var clientId = username;

        var application = await _applicationManager.FindByClientIdAsync(clientId);
        if (application == null)
            return AuthResult.Fail(Unauthorized(new { error = "Invalid credentials." }));

        var isValid = await _applicationManager.ValidateClientSecretAsync(application, password);
        if (!isValid)
            return AuthResult.Fail(Unauthorized(new { error = "Invalid credentials." }));

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var sp = await dbContext.Set<ServicePrincipal>()
            .FirstOrDefaultAsync(s => s.ClientId == clientId && s.OrganizationId == organizationId);

        if (sp == null || sp.IsDisabled)
            return AuthResult.Fail(Unauthorized(new { error = "Invalid credentials." }));

        var principalProvider = new LiteralPrincipalProvider(
            sp.Id,
            PrincipalDiscriminator.ServicePrincipal,
            new List<Guid> { organizationId });

        var service = _serviceFactory.Create(principalProvider);

        return AuthResult.Ok(sp.Id, organizationId, service);
    }

    private record AuthResult(IActionResult? ActionResult, Guid ServicePrincipalId, Guid OrganizationId, StateFileService? Service)
    {
        public static AuthResult Fail(IActionResult actionResult) => new(actionResult, Guid.Empty, Guid.Empty, null);
        public static AuthResult Ok(Guid servicePrincipalId, Guid organizationId, StateFileService service) => new(null, servicePrincipalId, organizationId, service);
    }

    private record TerraformLockInfo(string? ID, string? Operation, string? Info, string? Who, string? Version, string? Created, string? Path);
}
