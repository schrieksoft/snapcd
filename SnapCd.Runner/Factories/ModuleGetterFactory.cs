using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services;
using SnapCd.Runner.Services.ModuleGetter;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Factories;

public class ModuleGetterFactory
{
    private readonly IOptions<WorkingDirectorySettings> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GitFactory _gitFactory;
    private readonly HttpClient _httpClient;

    public ModuleGetterFactory(
        IOptions<WorkingDirectorySettings> options,
        GitFactory gitFactory,
        ILoggerFactory loggerFactory,
        HttpClient httpClient
    )
    {
        _options = options;
        _gitFactory = gitFactory;
        _loggerFactory = loggerFactory;
        _httpClient = httpClient;
    }


    public async Task<ModuleGetter> Create(
        RunnerTaskContext context,
        SourceType sourceType,
        SourceRevisionType sourceRevisionType,
        string sourceUrl,
        string sourceRevision,
        JobMetadata metadata,
        string engine = "tofu"
    )
    {
        var logger = _loggerFactory.CreateLogger<ModuleGetter>();

        var moduleDirectoryService = new ModuleDirectoryService(
            metadata,
            _options
        );

        switch (sourceType)
        {
            case SourceType.Git:
            {
                var git = _gitFactory.Create(context);
                return new GitModuleGetter(
                    sourceRevisionType,
                    sourceUrl,
                    sourceRevision,
                    metadata.SourceSubdirectory,
                    moduleDirectoryService,
                    context,
                    logger,
                    git,
                    engine
                );
            }
            case SourceType.Registry:
            {
                if (sourceRevisionType == SourceRevisionType.SemanticVersionRange)
                    throw new NotSupportedException(
                        $"Source Revision Type {nameof(SourceRevisionType.SemanticVersionRange)} is not supported in combination with Source Type {nameof(SourceType.Registry)}. It is currently only suport for Source Type {nameof(SourceType.Git)}.");

                var requestUrl = $"{sourceUrl}/{sourceRevision}/download";
                if (string.IsNullOrEmpty(sourceRevision))
                    requestUrl = $"{sourceUrl}/download";

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                // In this header the actual download location will be, such as "git::https://github.com/terraform-aws-modules/terraform-aws-iam?ref=304c8b5bbe99a1b17e1d24c96a4a384f108caced"
                var xTerraformGet = response.Headers.Single(x => x.Key.Equals("X-Terraform-Get")).Value.Single();

                // Parse the xTerraformGet string so that be can call "Create" again, this time to construct the concrete getter that we will use the fetch the actual data.
                var parsed = RegistrySourceParser.Parse(xTerraformGet);

                return await Create(
                    context,
                    parsed.SourceType,
                    SourceRevisionType.Default, // even if we are able to parse a semver range earlier, at this point parsed.Revision must already be resolved.
                    parsed.Url,
                    parsed.Revision ?? throw new InvalidOperationException("Revision could not be parsed out"),
                    metadata,
                    engine);
            }
            default:
                throw new NotImplementedException($"ModuleGetter for SourceType \"{sourceType}\" has not been implemented");
        }
    }
}