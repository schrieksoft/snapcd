// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace SnapCd.Server.Core.Telemetry;

/// <summary>The one renderer for What's New markdown, used by the page and by the admin preview so both show the same thing. Raw HTML is escaped and only http, https and mailto links survive.</summary>
public static class WhatsNewMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        renderer.ObjectRenderers.Replace<LinkInlineRenderer>(new SafeLinkRenderer());
        Pipeline.Setup(renderer);

        var document = Markdown.Parse(markdown, Pipeline);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    private sealed class SafeLinkRenderer : LinkInlineRenderer
    {
        private static readonly string[] AllowedSchemes = ["http:", "https:", "mailto:"];

        protected override void Write(HtmlRenderer renderer, LinkInline link)
        {
            var url = link.GetDynamicUrl?.Invoke() ?? link.Url ?? "";
            var trimmed = url.TrimStart();
            var colon = trimmed.IndexOf(':');
            var hasScheme = colon > 0 && trimmed[..colon].All(c => char.IsLetterOrDigit(c) || c is '+' or '-' or '.');
            if (hasScheme && !AllowedSchemes.Any(s => trimmed.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                link.Url = "";
                link.GetDynamicUrl = null;
            }
            if (link.IsImage)
            {
                renderer.WriteEscape(link.FirstChild?.ToString() ?? "");
                return;
            }
            base.Write(renderer, link);
        }
    }
}
