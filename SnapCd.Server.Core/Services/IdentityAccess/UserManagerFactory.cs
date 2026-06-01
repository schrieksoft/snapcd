// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.IdentityAccess;

public class UserManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UserManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ScopedService<UserManager<User>> Create()
    {
        var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        return new ScopedService<UserManager<User>>(userManager, scope);
    }
}

public class ScopedService<T> : IDisposable where T : class
{
    private readonly IServiceScope _scope;

    public T Service { get; }

    public ScopedService(T service, IServiceScope scope)
    {
        Service = service;
        _scope = scope;
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}