// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services;

public class AccessTokenServiceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbContextFactory<SnapCdDbContext> _dbFactory;
    private readonly IOptions<OpenIddictServerOptions> _options;
    private readonly IOptions<SecurityKeySettings> _securityKeyOptions;

    public AccessTokenServiceFactory(
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        IOptions<OpenIddictServerOptions> options,
        IOptions<SecurityKeySettings> securityKeyOptions
    )
    {
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _securityKeyOptions = securityKeyOptions;
    }

    public AccessTokenService Create()
    {
        return new AccessTokenService(_dbFactory.CreateDbContext(), _httpContextAccessor, _options, _securityKeyOptions);
    }
}

public class AccessTokenService : IDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OpenIddictServerOptions _options;
    private readonly SnapCdDbContext _dbContext;
    private readonly SecurityKeySettings _securityKeySettings;

    private const string AccessTokenType = "self_issued_access_token";
    private const string SubjectClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    //private readonly Guid _userId;

    public List<Token> Tokens { get; private set; }


    public AccessTokenService(
        SnapCdDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IOptions<OpenIddictServerOptions> options,
        IOptions<SecurityKeySettings> securityKeySettings
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _dbContext = dbContext;
        _securityKeySettings = securityKeySettings.Value;
        Tokens = GetAccessTokens();
    }


    // see https://stackoverflow.com/questions/77408197/how-do-i-generate-a-token-internally-in-openiddict-in-order-to-call-other-apis
    public GeneratedToken GenerateToken(string name, DateTime expirationDateTime, params string[] scopes)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            var claimsIdentity = _httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;

            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
                return GenerateToken(claimsIdentity, name, expirationDateTime, scopes);
        }

        throw new UnauthorizedAccessException("No authenticated ClaimsIdentity found in HttpContext");
    }

    public GeneratedToken GenerateToken(ClaimsIdentity claimsIdentity, string name, DateTime expirationDateTime,
        params string[] scopes)
    {
        var userId = GetSubject(claimsIdentity);

        var updatedClaimsIdentity = CreateUpdatedClaimsIdentity(claimsIdentity);

        var claims = scopes.Select(a => new Claim(OpenIddictConstants.Claims.Scope, a))
            .ToList(); //TODO might need to add some more claims here

        //unique identifier with which this specific token can be looked up in Database
        var jti = Guid.NewGuid();
        claims.Add(new Claim("jti", jti.ToString()));
        var tokenId = Guid.NewGuid();
        claims.Add(new Claim("oi_tkn_id", tokenId.ToString()));
        claims.Add(new Claim("principal_discriminator", "User"));

        var issuedAtDateTime = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims.ToDictionary(a => a.Type, a => (object)a.Value),
            Expires = expirationDateTime,
            Audience = "snapcd",
            IssuedAt = issuedAtDateTime,
            Issuer = _options.Issuer!.ToString(),
            SigningCredentials =
                new SigningCredentials(_securityKeySettings.RsaSigningPrivateKey,
                    SecurityAlgorithms.RsaSha256), //  _options.SigningCredentials.First(),
            EncryptingCredentials = new EncryptingCredentials(_securityKeySettings.SymmetricEncryptionKey,
                SecurityAlgorithms.Aes256KW,
                SecurityAlgorithms.Aes256CbcHmacSha512), //_options.EncryptionCredentials.First(),
            Subject = updatedClaimsIdentity,
            TokenType = OpenIddictConstants.JsonWebTokenTypes.AccessToken
        };
        var tokenString = new JsonWebTokenHandler().CreateToken(descriptor);

        var tokenEntity = new Token
        {
            Name = name,
            Id = tokenId,
            Subject = userId.ToString(),
            ExpirationDate = expirationDateTime,
            CreationDate = issuedAtDateTime,
            Status = "valid",
            Type = AccessTokenType,
            ConcurrencyToken = Guid.NewGuid().ToString()
        };

        return new GeneratedToken
        {
            TokenString = tokenString,
            TokenEntity = tokenEntity
        };
    }

    public List<Token> GetAccessTokens()
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            var claimsIdentity = _httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var userId = GetSubject(claimsIdentity);
                if (UserIsActive(userId))
                {
                    var tokens = _dbContext.Tokens
                        .Where(x => x.Subject == userId.ToString() && x.Type == AccessTokenType).ToList();

                    return tokens;
                }
            }
        }

        return new List<Token>();
    }

    public void AddAccessToken(Token token)
    {
        _dbContext.Tokens.Add(token);
        _dbContext.SaveChanges();

        Tokens = GetAccessTokens();
    }

    public void RevokeAccessToken(Guid tokenId)
    {
        _dbContext.Tokens.Remove(Tokens.Single(x => x.Id == tokenId));
        _dbContext.SaveChanges();

        Tokens = GetAccessTokens();
    }


    private Guid GetSubject(ClaimsIdentity claimsIdentity)
    {
        var stringClaim = claimsIdentity.Claims.Single(c => c.Type == SubjectClaimType).Value;

        return new Guid(stringClaim);
    }

    private bool UserIsActive(Guid userId)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return false;

        return user.IsDisabled != true;
    }

    private ClaimsIdentity CreateUpdatedClaimsIdentity(ClaimsIdentity identity)
    {
        // Collect updated claims
        var updatedClaims = identity.Claims
            .Where(claim =>
                claim.Type != "AspNet.Identity.SecurityStamp") // Exclude this claim as it is a secret value.
            .Select(claim => new Claim(
                claim.Type switch
                {
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => JwtRegisteredClaimNames
                        .Sub,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" => "email",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => "name",
                    _ => claim.Type // Keep the original type if no mapping is needed
                },
                claim.Value,
                claim.ValueType,
                claim.Issuer,
                claim.OriginalIssuer
            )).ToList();

        // Create a new ClaimsIdentity with the updated claims
        return new ClaimsIdentity(updatedClaims, identity.AuthenticationType, identity.NameClaimType,
            identity.RoleClaimType);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}