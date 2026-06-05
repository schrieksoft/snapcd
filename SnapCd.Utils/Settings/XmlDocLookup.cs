// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace SnapCd.Utils.Settings;

/// <summary>
/// Loads an XML doc file produced by the C# compiler and resolves <c>&lt;summary&gt;</c> trivia for
/// types and properties via the standard XML doc member-id grammar (<c>T:</c> for types,
/// <c>P:</c> for properties).
/// </summary>
public sealed class XmlDocLookup
{
    private readonly Dictionary<string, string> _summaries;

    private XmlDocLookup(Dictionary<string, string> summaries) => _summaries = summaries;

    public static XmlDocLookup Load(string xmlPath)
    {
        var doc = new XmlDocument();
        doc.Load(xmlPath);

        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);
        var members = doc.SelectNodes("/doc/members/member");
        if (members is null) return new XmlDocLookup(summaries);

        foreach (XmlNode member in members)
        {
            var name = member.Attributes?["name"]?.Value;
            if (string.IsNullOrEmpty(name)) continue;

            var summary = member.SelectSingleNode("summary")?.InnerXml;
            if (string.IsNullOrWhiteSpace(summary)) continue;

            summaries[name] = NormaliseWhitespace(summary);
        }

        return new XmlDocLookup(summaries);
    }

    public string? GetSummary(MemberInfo member) => member switch
    {
        Type t => Get($"T:{FormatTypeName(t)}"),
        PropertyInfo p => Get($"P:{FormatTypeName(p.DeclaringType!)}.{p.Name}"),
        _ => null,
    };

    private string? Get(string key) => _summaries.TryGetValue(key, out var s) ? s : null;

    private static string FormatTypeName(Type type)
    {
        // Generic types use a backtick-arity form in XML doc ids — Foo`2.Bar etc. The settings types
        // we walk are non-generic POCOs, so the simple FullName works; revisit if generics appear.
        return type.FullName!.Replace('+', '.');
    }

    private static readonly Regex SeeCrefPattern = new(
        @"<see\s+cref=""([^""]+)""\s*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string NormaliseWhitespace(string raw)
    {
        // <see cref="..."/> cross-references must be substituted with the referent's last identifier
        // segment before the generic tag-stripper runs, or "even before <see cref="PeriodSeconds"/>
        // elapses" becomes "even before  elapses" — drops the symbol entirely.
        var withCrefs = SeeCrefPattern.Replace(raw, static m =>
        {
            var cref = m.Groups[1].Value;
            // C# XML doc cref format: prefix-letter ':' then a dotted FQN. Prefixes: T (type),
            // P (property), M (method), F (field), N (namespace), E (event). Trailing
            // parens carry method args (M:Foo.Bar(System.String)). Strip both, then take the
            // last dotted segment.
            var colon = cref.IndexOf(':');
            if (colon >= 0 && colon + 1 < cref.Length) cref = cref[(colon + 1)..];
            var paren = cref.IndexOf('(');
            if (paren >= 0) cref = cref[..paren];
            var lastDot = cref.LastIndexOf('.');
            if (lastDot >= 0 && lastDot + 1 < cref.Length) cref = cref[(lastDot + 1)..];
            return cref;
        });

        // After cref substitution, strip any remaining tags (<para>, <c>, <paramref>, etc.).
        var withoutTags = Regex.Replace(withCrefs, "<[^>]+>", " ");
        var collapsed = Regex.Replace(withoutTags, @"\s+", " ");
        // Stripping tags can leave " . " where "<c>x</c>." became " x .". Drop spaces before
        // terminal punctuation only when the punctuation isn't immediately followed by a word
        // character — otherwise " .NET" (intentional leading-dot identifier) gets eaten into
        // ".NET" attached to the previous word.
        var tightenedPunctuation = Regex.Replace(collapsed, @"\s+([.,;:!?])(?!\w)", "$1");
        return tightenedPunctuation.Trim();
    }
}
