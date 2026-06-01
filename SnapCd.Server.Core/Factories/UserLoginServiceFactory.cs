// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.IdentityAccess;

namespace SnapCd.Server.Core.Factories;

public class UserLoginServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UserLoginServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ScopedService<UserLoginService> Create()
    {
        var scope = _serviceProvider.CreateScope();

        // Get all required dependencies from the scoped service provider
        var service = scope.ServiceProvider.GetRequiredService<UserLoginService>();

        return new ScopedService<UserLoginService>(service, scope);
    }

    public (UserLoginService Repository, IServiceScope Scope) CreateWithScope()
    {
        var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<UserLoginService>();

        return (service, scope);
    }
}