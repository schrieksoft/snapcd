// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using System.Text.RegularExpressions;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Architecture;

/// <summary>
/// The service tree is sorted by what a type reaches and whether that access is secured. None of
/// it is enforced by the compiler: an illegal reference builds, and a Blazor component resolves
/// its injections at render time rather than at startup, so the container's own validation never
/// sees them. These read the source instead.
/// </summary>
public class ServiceLayeringTests
{
    /// <summary>
    /// A component names its injections in markup, so a service it asks for that nothing registers
    /// is a crash on first render of that page and nothing earlier. Startup validation walks the
    /// registrations, which a component is not among.
    /// </summary>
    [Fact]
    public void Every_Service_A_Component_Injects_Is_Registered()
    {
        var declared = DeclaredServiceTypes();
        var registered = RegisteredTypes();

        var unregistered = RazorFiles()
            .SelectMany(file => Injections(file).Select(type => (file, type)))
            .Where(x => declared.Contains(x.type) && !registered.Contains(x.type))
            .Select(x => $"{Path.GetFileName(x.file)} injects {x.type}")
            .Distinct()
            .OrderBy(line => line)
            .ToList();

        Assert.Empty(unregistered);
    }

    /// <summary>
    /// A component holds its injections for the life of the circuit, which outlives the request
    /// that opened it. A service holding a DbContext is therefore reached through its factory and
    /// built per using block, so the component decides how long the context lives.
    /// </summary>
    [Fact]
    public void A_Component_Reaches_A_Context_Holding_Service_Through_Its_Factory()
    {
        var byFactory = ServicesWithAFactoryAndAContext();

        var injectedDirectly = RazorFiles()
            .SelectMany(file => Injections(file).Select(type => (file, type)))
            .Where(x => byFactory.Contains(x.type))
            .Select(x => $"{Path.GetFileName(x.file)} injects {x.type} instead of {x.type}Factory")
            .Distinct()
            .OrderBy(line => line)
            .ToList();

        Assert.Empty(injectedDirectly);
    }

    /// <summary>
    /// Only a factory takes a factory. A factory handed a scoped context gives every instance it
    /// builds the same one, so two of them in flight at once are two operations on one context and
    /// EF refuses the second. A component rendering several log panels does exactly that.
    /// </summary>
    [Fact]
    public void A_Factory_Takes_A_Context_Factory_Rather_Than_A_Context()
    {
        // Identity's UserClaimsPrincipalFactory is a framework contract that happens to end in
        // Factory, so the name is not a claim about what it holds.
        string[] exempt = ["CustomUserClaimsPrincipalFactory"];

        var offenders = new List<string>();

        foreach (var file in SourceFiles(Path.Combine(SolutionRoot, "SnapCd.Server.Core")))
        {
            var body = File.ReadAllText(file);

            foreach (Match match in Regex.Matches(
                         body, @"public\s+(?:sealed\s+)?class\s+(\w*Factory)\s*\(([^)]*)\)", RegexOptions.Singleline))
            {
                var parameters = match.Groups[2].Value;
                if (exempt.Contains(match.Groups[1].Value)) continue;
                if (Regex.IsMatch(parameters, @"\bSnapCdDbContext\s+\w+") && !parameters.Contains("IDbContextFactory"))
                    offenders.Add($"{Path.GetFileName(file)}: {match.Groups[1].Value}");
            }

            foreach (Match match in Regex.Matches(
                         body, @"public\s+(\w*Factory)\s*\(([^)]*)\)\s*(?::[^{]*)?\{", RegexOptions.Singleline))
            {
                var parameters = match.Groups[2].Value;
                if (exempt.Contains(match.Groups[1].Value)) continue;
                if (Regex.IsMatch(parameters, @"\bSnapCdDbContext\s+\w+") && !parameters.Contains("IDbContextFactory"))
                    offenders.Add($"{Path.GetFileName(file)}: {match.Groups[1].Value}");
            }
        }

        Assert.Empty(offenders.Distinct().OrderBy(line => line));
    }

    /// <summary>
    /// The other half of the same rule: a service takes the context, not the means of making one.
    /// A service holding a factory decides for itself how long a context lives, so it cannot share
    /// the caller's transaction, which is the whole point of handing it one. The exemptions are
    /// types with no scope to draw from or a reason to need a context per read, and they are listed
    /// here so that adding another is a decision rather than a drift.
    /// </summary>
    [Fact]
    public void Only_A_Factory_Holds_A_Context_Factory()
    {
        string[] exempt =
        [
            "MaintenanceModeService",               // singleton
            "OrganizationMembershipCacheService",   // singleton
            "SecretMigratorService",                // Parallel.ForEachAsync: a context serves one thread
            "SqlVault",                             // shared across that same parallel loop
            "CheckRunnerConnectionActivity"         // polls for a row another connection writes
        ];

        var offenders = new List<string>();

        foreach (var file in SourceFiles(Path.Combine(SolutionRoot, "SnapCd.Server.Core")))
        {
            var body = Strip(File.ReadAllText(file));
            if (!body.Contains("IDbContextFactory<SnapCdDbContext>")) continue;

            foreach (Match match in Regex.Matches(body, @"IDbContextFactory<SnapCdDbContext>\s+_?\w+"))
            {
                var declarations = Regex.Matches(
                    body[..match.Index],
                    @"(?:public|internal)\s+(?:sealed\s+|abstract\s+|partial\s+|static\s+)*(?:class|record)\s+(\w+)");

                if (declarations.Count == 0) continue;
                var owner = declarations[^1].Groups[1].Value;

                if (owner.EndsWith("Factory") || owner.Contains("Repository") || exempt.Contains(owner)) continue;

                offenders.Add($"{Path.GetFileName(file)}: {owner}");
            }
        }

        Assert.Empty(offenders.Distinct().OrderBy(line => line));
    }

    /// <summary>
    /// Services/Utils is a leaf. A file there that reaches the data layer is not a utility, it is
    /// a participant that happens not to hold a context itself, and it belongs beside the types it
    /// calls. Checked by type name rather than by using, because the namespaces do not follow the
    /// folders yet.
    /// </summary>
    [Fact]
    public void Nothing_In_Utils_Reaches_The_Data_Layer()
    {
        var dataTypes = DeclaredIn("Secured").Union(DeclaredIn("Nonsecured")).ToHashSet();

        var offenders = new List<string>();
        foreach (var file in SourceFiles(Path.Combine(ServicesDirectory, "Utils")))
        {
            var body = Strip(File.ReadAllText(file));
            var own = TypeNames(body).ToHashSet();
            var reached = Identifiers(body).Where(name => dataTypes.Contains(name) && !own.Contains(name)).Distinct();

            foreach (var name in reached)
                offenders.Add($"{Path.GetFileName(file)} references {name}");
        }

        Assert.Empty(offenders.OrderBy(line => line));
    }

    /// <summary>An interface describes a contract; Utils holds implementations.</summary>
    [Fact]
    public void No_Interface_Is_Declared_Under_Utils()
    {
        var offenders = SourceFiles(Path.Combine(ServicesDirectory, "Utils"))
            .Where(file => Regex.IsMatch(Strip(File.ReadAllText(file)), @"^\s*(public|internal)\s+interface\s", RegexOptions.Multiline))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(offenders);
    }

    /// <summary>A base class follows its subclasses, which hold the context it does not.</summary>
    [Fact]
    public void No_Abstract_Class_Is_Declared_Under_Utils()
    {
        var offenders = SourceFiles(Path.Combine(ServicesDirectory, "Utils"))
            .Where(file => Regex.IsMatch(Strip(File.ReadAllText(file)), @"^\s*(public|internal)\s+abstract\s+(partial\s+)?class\s", RegexOptions.Multiline))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToList();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Render mode is decided centrally, by matching the request path against the allow-list in
    /// RenderModeHelper: on the list is static, everything else is interactive. A static page holds
    /// nothing past the response, so injecting a service directly there is fine. An interactive
    /// component is held for the life of the circuit, so a service it injects is held that long too,
    /// and a context inside one outlives every request it was meant to serve.
    ///
    /// The context need not be the service's own field. A service holding another service that holds
    /// one keeps it just as long, so this follows the chain.
    /// </summary>
    [Fact]
    public void No_Interactive_Component_Injects_A_Context_Holding_Service()
    {
        var holders = ContextHoldingServices();

        var offenders = InteractiveReachableComponents()
            .SelectMany(file => Injections(file).Select(type => (file, type)))
            .Where(x => holders.ContainsKey(x.type))
            .Select(x => $"{Path.GetFileName(x.file)} injects {x.type} ({holders[x.type]})")
            .Distinct()
            .OrderBy(line => line)
            .ToList();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Every service type that ends up holding a SnapCdDbContext, mapped to why. A type holding
    /// IDbContextFactory is not one of these: it builds a context per operation, which is the
    /// pattern being asked for.
    /// </summary>
    private static Dictionary<string, string> ContextHoldingServices()
    {
        var fields = new Dictionary<string, HashSet<string>>();
        var direct = new Dictionary<string, string>();

        foreach (var file in SourceFiles(ServicesDirectory))
        {
            var body = Strip(File.ReadAllText(file));

            foreach (Match match in Regex.Matches(body,
                         @"^\s*(?:public|internal)\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)",
                         RegexOptions.Multiline))
            {
                var name = match.Groups[1].Value;
                if (name.EndsWith("Factory", StringComparison.Ordinal)) continue;

                // the ctor parameters and fields this type keeps, primary constructor included
                var held = Regex.Matches(body, @"\b([A-Z]\w*)\s+\w+\s*[,){;=]")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet();

                if (held.Contains("SnapCdDbContext") && !body.Contains("IDbContextFactory<SnapCdDbContext>"))
                    direct[name] = "holds SnapCdDbContext";

                fields[name] = held;
            }
        }

        // a service holding a service that holds a context keeps it just as long
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var (name, held) in fields)
            {
                if (direct.ContainsKey(name)) continue;
                var via = held.FirstOrDefault(h => h != name && direct.ContainsKey(h));
                if (via == null) continue;
                direct[name] = $"holds {via}, which holds a context";
                changed = true;
            }
        }

        return direct;
    }

    /// <summary>
    /// The components an interactive page can reach: the pages not on the static allow-list, plus
    /// every component and layout they name, transitively.
    /// </summary>
    private static HashSet<string> InteractiveReachableComponents()
    {
        var staticRoutes = StaticRoutes();
        var byName = RazorFiles()
            .GroupBy(f => Path.GetFileNameWithoutExtension(f))
            .ToDictionary(g => g.Key, g => g.ToList());

        var interactive = RazorFiles().Where(file =>
        {
            var routes = Regex.Matches(File.ReadAllText(file), @"^@page\s+""([^""]+)""", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value).ToList();
            return routes.Count > 0
                && !routes.Any(r => staticRoutes.Any(s => r.StartsWith(s, StringComparison.OrdinalIgnoreCase)));
        }).ToList();

        var reached = new HashSet<string>(interactive);
        var pending = new Stack<string>(interactive);

        while (pending.Count > 0)
        {
            var body = File.ReadAllText(pending.Pop());

            var names = Regex.Matches(body, @"<([A-Z][A-Za-z0-9_]*)[\s/>]").Select(m => m.Groups[1].Value)
                .Concat(Regex.Matches(body, @"^@layout\s+([A-Za-z0-9_]+)", RegexOptions.Multiline).Select(m => m.Groups[1].Value));

            foreach (var name in names)
            {
                if (!byName.TryGetValue(name, out var candidates)) continue;
                foreach (var candidate in candidates.Where(c => reached.Add(c)))
                    pending.Push(candidate);
            }
        }

        return reached;
    }

    /// <summary>The static allow-list, read from the helper that decides render mode.</summary>
    private static List<string> StaticRoutes()
    {
        var helper = File.ReadAllText(Path.Combine(ServicesDirectory, "Utils", "Dashboard", "RenderModeHelper.cs"));
        var block = helper.Split("IsStaticSsr =")[1].Split("};")[0];

        return Regex.Matches(block, @"""([^""]+)""").Select(m => m.Groups[1].Value).ToList();
    }

    private static readonly Regex InjectPattern =
        new(@"^@inject\s+([A-Za-z0-9_.<>]+)\s+\w+", RegexOptions.Multiline);

    private static readonly Regex TypePattern =
        new(@"^\s*(?:public|internal)\s+(?:sealed\s+|static\s+|abstract\s+|partial\s+)*(?:class|record|struct|interface|enum)\s+([A-Z]\w*)",
            RegexOptions.Multiline);

    private static readonly Regex RegistrationPattern =
        new(@"Add(?:Scoped|Singleton|Transient|HttpClient|DbContextFactory|DbContext)<([^>]+)>");

    private static IEnumerable<string> Injections(string razorFile) =>
        InjectPattern.Matches(File.ReadAllText(razorFile))
            .Select(m => Simple(m.Groups[1].Value));

    /// <summary>The name without its namespace or type arguments, which is how markup names it.</summary>
    private static string Simple(string name)
    {
        var withoutGenerics = name.Split('<')[0];
        return withoutGenerics.Split('.').Last();
    }

    private static HashSet<string> RegisteredTypes() =>
        SourceFiles(Path.Combine(SolutionRoot, "SnapCd.Server.Core", "Startup"))
            .Concat(SourceFiles(Path.Combine(SolutionRoot, "SnapCd.Server.Host")))
            .SelectMany(file => RegistrationPattern.Matches(File.ReadAllText(file)))
            .SelectMany(m => m.Groups[1].Value.Split(','))
            .Select(Simple)
            .ToHashSet();

    /// <summary>Every type declared under Services, which is what these rules are about.</summary>
    private static HashSet<string> DeclaredServiceTypes() =>
        SourceFiles(ServicesDirectory)
            .SelectMany(file => TypeNames(Strip(File.ReadAllText(file))))
            .ToHashSet();

    /// <summary>
    /// A service that both has a sibling factory and holds a context: the pair a component has to
    /// reach through the factory.
    /// </summary>
    private static HashSet<string> ServicesWithAFactoryAndAContext()
    {
        var pairs = new HashSet<string>();

        foreach (var file in SourceFiles(ServicesDirectory))
        {
            var body = File.ReadAllText(file);
            if (!body.Contains("SnapCdDbContext")) continue;

            foreach (Match match in Regex.Matches(body, @"^\s*public\s+(?:sealed\s+)?class\s+(\w+)Factory\b", RegexOptions.Multiline))
            {
                var service = match.Groups[1].Value;
                if (Regex.IsMatch(body, $@"^\s*public\s+(?:sealed\s+)?class\s+{service}\b", RegexOptions.Multiline))
                    pairs.Add(service);
            }
        }

        return pairs;
    }

    private static HashSet<string> DeclaredIn(string folder) =>
        SourceFiles(Path.Combine(ServicesDirectory, folder))
            .SelectMany(file => TypeNames(Strip(File.ReadAllText(file))))
            .ToHashSet();

    private static IEnumerable<string> TypeNames(string body) =>
        TypePattern.Matches(body).Select(m => m.Groups[1].Value);

    private static IEnumerable<string> Identifiers(string body) =>
        Regex.Matches(body, @"\b[A-Z]\w*\b").Select(m => m.Value);

    /// <summary>Comments out, so a type named only in a doc comment does not read as a reference.</summary>
    private static string Strip(string source)
    {
        source = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        source = Regex.Replace(source, @"^\s*///.*$", string.Empty, RegexOptions.Multiline);

        return string.Join('\n', source.Split('\n').Select(line =>
        {
            var comment = line.IndexOf("//", StringComparison.Ordinal);
            if (comment < 0) return line;
            var before = line[..comment];
            return before.Count(c => c == '"') % 2 == 0 ? before : line;
        }));
    }

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(SolutionRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static IEnumerable<string> SourceFiles(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            : [];

    private static string ServicesDirectory =>
        Path.Combine(SolutionRoot, "SnapCd.Server.Core", "Services");

    private static string SolutionRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SnapCd.Server.Core")))
                directory = directory.Parent;

            Assert.NotNull(directory);
            return directory!.FullName;
        }
    }
}
