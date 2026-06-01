// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Factories;

public class SignInManagerFactory
{
    private readonly UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> _userManagerFactory;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IUserClaimsPrincipalFactory<User> _claimsFactory;
    private readonly IOptions<IdentityOptions> _optionsAccessor;
    private readonly ILogger<SignInManager<User>> _logger;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IUserConfirmation<User> _confirmation;

    public SignInManagerFactory(
        UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> userManagerFactory,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<User> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<User>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<User> confirmation)
    {
        _userManagerFactory = userManagerFactory;
        _contextAccessor = contextAccessor;
        _claimsFactory = claimsFactory;
        _optionsAccessor = optionsAccessor;
        _logger = logger;
        _schemes = schemes;
        _confirmation = confirmation;
    }

    public SignInManagerScope Create()
    {
        var userManagerScope = _userManagerFactory.Create();
        var signInManager = new SignInManager<User>(
            userManagerScope.UserManager,
            _contextAccessor,
            _claimsFactory,
            _optionsAccessor,
            _logger,
            _schemes,
            _confirmation);

        return new SignInManagerScope(signInManager, userManagerScope);
    }
}

public class SignInManagerScope : IDisposable
{
    public SignInManager<User> SignInManager { get; }
    private readonly UserManagerScope<User> _userManagerScope;

    internal SignInManagerScope(SignInManager<User> signInManager, UserManagerScope<User> userManagerScope)
    {
        SignInManager = signInManager;
        _userManagerScope = userManagerScope;
    }

    public void Dispose()
    {
        _userManagerScope?.Dispose();
    }
}