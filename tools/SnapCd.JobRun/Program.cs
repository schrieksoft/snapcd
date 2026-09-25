// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts;
using SnapCd.JobRun;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Startup;

var options = RunOptions.Parse(args);
if (options is null) return 1;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile(options.ServerAppSettingsPath, optional: false)
    .AddInMemoryCollection(options.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(c => c.SingleLine = true);
builder.Logging.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Warning);

ServerComposition.AddServerServices(builder.Services, builder.Configuration, options.ConnectionString);

builder.Services.Replace(ServiceDescriptor.Scoped<IPrincipalProvider>(_ => new LiteralPrincipalProvider(
    options.PrincipalId, PrincipalDiscriminator.User, [options.OrganizationId])));

// The runner is answered in-process, so the dispatch seam is replaced rather than the hub itself.
builder.Services.AddSingleton(sp => new FakeRunner(
    sp, options.ConnectionId, options.OrganizationId, options.RunnerInstanceName,
    line => { if (options.Verbose) Console.WriteLine(line); }));
builder.Services.Replace(
    ServiceDescriptor.Singleton<IHubContext<RunnerHub>>(
        sp => new FakeHubContext(sp.GetRequiredService<FakeRunner>())));
builder.Services.AddScoped<RunnerHub>();

// The database has to exist before the host starts: the transport migrates itself on startup.
Console.WriteLine($"Database    {options.DatabaseName}");
Console.WriteLine($"App db      {builder.Configuration["ConnectionString"]}");
Console.WriteLine($"Bus db      {builder.Configuration["ServiceBus:TransportOptions:SqlServer:ConnectionString"]}");
await ScratchDatabase.Create(options);

try
{
    var app = builder.Build();

    await ScratchDatabase.Prepare(app.Services);
    await app.StartAsync();

    try
    {
        return await Run.Execute(app.Services, options);
    }
    finally
    {
        await app.StopAsync();
    }
}
finally
{
    if (options.Keep)
        Console.WriteLine($"Kept        {options.DatabaseName}");
    else
        await ScratchDatabase.Drop(options);
}
