// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;
using SnapCd.Contracts;

namespace SnapCd.Runner.Services.ModuleGetter;

public static class RegistrySourceParser
{
    public static ParsedSource Parse(string source)
    {
        var originalSource = source;
        var queryParameters = new Dictionary<string, string>();
        SourceType sourceType;

        // Check for explicit protocol (e.g., "git::", "s3::")
        var protocolMatch = Regex.Match(source, @"^(?<protocol>[a-z]+)::(.+)$");
        if (protocolMatch.Success)
        {
            sourceType = ParseProtocol(protocolMatch.Groups["protocol"].Value);
            source = source.Substring(protocolMatch.Groups["protocol"].Value.Length + 2);
        }
        else
        {
            // Infer protocol from URL structure
            sourceType = InferProtocol(source);
        }

        // Split into URL, subdirectory, and query parameters
        var parts = SplitSource(source);

        var parsed = new ParsedSource
        {
            Url = parts.BaseUrl,
            OriginalSource = originalSource,
            QueryParameters = queryParameters,
            SourceType = sourceType,
            Revision = InferRevision(parts.BaseUrl, parts.QueryParameters, sourceType)
        };

        return parsed;
    }

    private static SourceType ParseProtocol(string protocol)
    {
        return protocol.ToLower() switch
        {
            "git" => SourceType.Git,
            "s3" => SourceType.S3,
            "http" => SourceType.Http,
            "https" => SourceType.Https,
            "gcs" => SourceType.Gcs,
            "hg" => SourceType.Mercurial,
            _ => SourceType.Unknown
        };
    }

    private static SourceType InferProtocol(string source)
    {
        // Check for Terraform Registry format (e.g., "hashicorp/consul/aws")
        if (Regex.IsMatch(source, @"^[a-zA-Z0-9-]+/[a-zA-Z0-9-]+/[a-zA-Z0-9-]+$")) return SourceType.Registry;

        // Check for known Git hosts
        if (Regex.IsMatch(source, @"^(github\.com|gitlab\.com|bitbucket\.org|azure\.com/)")) return SourceType.Git;

        // Check for HTTP/HTTPS
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return SourceType.Http;
        if (source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return SourceType.Https;

        return SourceType.Unknown;
    }

    private static (string BaseUrl, Dictionary<string, string> QueryParameters) SplitSource(string source)
    {
        var baseUrl = source;
        var queryParams = new Dictionary<string, string>();

        // Extract query parameters
        var queryIndex = baseUrl.IndexOf('?');
        if (queryIndex >= 0)
        {
            var queryString = baseUrl.Substring(queryIndex + 1);
            baseUrl = baseUrl.Substring(0, queryIndex);

            foreach (var param in queryString.Split('&'))
            {
                var keyValue = param.Split('=', 2);
                if (keyValue.Length == 2) queryParams[keyValue[0]] = keyValue[1];
            }
        }


        return (baseUrl, queryParams);
    }


    private static string? InferRevision(string _, Dictionary<string, string> queryParameters, SourceType sourceType)
    {
        switch (sourceType)
        {
            case SourceType.Git:
                return queryParameters.GetValueOrDefault("ref");
            default:
                throw new NotImplementedException($"SourceType {sourceType} not implemented");
        }
    }
}

public class ParsedSource
{
    public required SourceType SourceType { get; set; }
    public required string OriginalSource { get; set; }
    public required Dictionary<string, string> QueryParameters { get; set; }


    public required string Url { get; set; }

    public string? Revision { get; set; }

    public string? ResolvedUrl { get; set; } // For Terraform Registry resolution
}