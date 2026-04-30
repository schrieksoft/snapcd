using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Factories;

public class EngineFactory
{
    private readonly IOptions<WorkingDirectorySettings> _workingDirectorySettings;
    private readonly IOptions<EngineSettings> _engineSettings;
    private readonly ILoggerFactory _loggerFactory;

    public EngineFactory(
        IOptions<WorkingDirectorySettings> workingDirectorySettings,
        IOptions<EngineSettings> engineSettings,
        ILoggerFactory loggerFactory
    )
    {
        _workingDirectorySettings = workingDirectorySettings;
        _engineSettings = engineSettings;
        _loggerFactory = loggerFactory;
    }

    public IEngine Create(
        RunnerTaskContext context,
        string engine,
        JobMetadata metadata,
        List<PulumiFlagEntry>? pulumiFlags = null,
        List<PulumiArrayFlagEntry>? pulumiArrayFlags = null,
        List<TerraformFlagEntry>? terraformFlags = null,
        List<TerraformArrayFlagEntry>? terraformArrayFlags = null)
    {
        var moduleDirectoryService = new ModuleDirectoryService(
            metadata,
            _workingDirectorySettings
        );

        var additionalBinaryPaths = _engineSettings.Value.AdditionalBinaryPaths;

        List<EngineFlagEntry> engineFlags;
        List<EngineArrayFlagEntry> engineArrayFlags;

        switch (engine)
        {
            case "pulumi":
                engineFlags = PulumiFlagConverter.Convert(pulumiFlags ?? []);
                engineArrayFlags = PulumiFlagConverter.Convert(pulumiArrayFlags ?? []);
                break;
            case "terraform" or "tofu":
                engineFlags = TerraformFlagConverter.Convert(terraformFlags ?? []);
                engineArrayFlags = TerraformFlagConverter.Convert(terraformArrayFlags ?? []);
                break;
            default:
                engineFlags = [];
                engineArrayFlags = [];
                break;
        }

        return engine switch
        {
            "terraform" or "tofu" => new TerraformEngine(
                context,
                _loggerFactory.CreateLogger<TerraformEngine>(),
                moduleDirectoryService,
                engine,
                additionalBinaryPaths,
                engineFlags,
                engineArrayFlags),
            "pulumi" => new PulumiEngine(
                context,
                _loggerFactory.CreateLogger<PulumiEngine>(),
                moduleDirectoryService,
                additionalBinaryPaths,
                engineFlags,
                engineArrayFlags),
            _ => throw new NotSupportedException($"Engine '{engine}' is not supported")
        };
    }
}