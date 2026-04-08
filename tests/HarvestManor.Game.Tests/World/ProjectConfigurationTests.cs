using System.Text.RegularExpressions;
using System.Runtime.Loader;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class ProjectConfigurationTests
{
    [Fact]
    public void CsprojSdkVersion_MatchesProjectGodotFeatureVersion()
    {
        var csprojContents = File.ReadAllText(FindFileInRepo("game", "HarvestManor.csproj"));
        var projectContents = File.ReadAllText(FindFileInRepo("game", "project.godot"));

        var sdkVersion = ExtractFirstMatch(
            csprojContents,
            "Godot\\.NET\\.Sdk/(?<version>\\d+\\.\\d+\\.\\d+)",
            "version");

        var godotFeatureVersion = ExtractFirstMatch(
            projectContents,
            "config/features=PackedStringArray\\(\"(?<version>\\d+\\.\\d+)\"",
            "version");

        Assert.StartsWith(godotFeatureVersion + ".", sdkVersion, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDotnetAssemblyName_MatchesCsprojAssemblyName()
    {
        var csprojContents = File.ReadAllText(FindFileInRepo("game", "HarvestManor.csproj"));
        var projectContents = File.ReadAllText(FindFileInRepo("game", "project.godot"));

        var csprojAssemblyName = ExtractFirstMatch(
            csprojContents,
            "<AssemblyName>(?<name>[^<]+)</AssemblyName>",
            "name");

        var projectAssemblyName = ExtractFirstMatch(
            projectContents,
            "project/assembly_name=\"(?<name>[^\"]+)\"",
            "name");

        Assert.Equal(csprojAssemblyName, projectAssemblyName);
    }

    [Fact]
    public void BuiltGameAssembly_ContainsGameBootstrapType()
    {
        var assemblyPath = FindFileInRepo(
            "game",
            ".godot",
            "mono",
            "temp",
            "bin",
            "Debug",
            "HarvestManor.dll");

        var loadContext = new ProjectAssemblyLoadContext(assemblyPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            Assert.NotNull(assembly.GetType("HarvestManor.World.GameBootstrap", throwOnError: false));
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public void GameBootstrap_HasGodotScriptPathAttribute()
    {
        var assemblyPath = FindFileInRepo(
            "game",
            ".godot",
            "mono",
            "temp",
            "bin",
            "Debug",
            "HarvestManor.dll");

        var loadContext = new ProjectAssemblyLoadContext(assemblyPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var gameBootstrapType = assembly.GetType("HarvestManor.World.GameBootstrap", throwOnError: false);
            Assert.NotNull(gameBootstrapType);
            var scriptPathAttribute = gameBootstrapType.GetCustomAttributesData()
                .FirstOrDefault(attribute => attribute.AttributeType.FullName == "Godot.ScriptPathAttribute");

            Assert.NotNull(scriptPathAttribute);
            Assert.Equal(
                "res://scripts/world/GameBootstrap.cs",
                Assert.Single(scriptPathAttribute.ConstructorArguments).Value as string);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public void BuiltGameAssembly_AllTypesLoadSuccessfully()
    {
        var assemblyPath = FindFileInRepo(
            "game",
            ".godot",
            "mono",
            "temp",
            "bin",
            "Debug",
            "HarvestManor.dll");

        var loadContext = new ProjectAssemblyLoadContext(assemblyPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var exception = Record.Exception(() => assembly.GetTypes());
            Assert.Null(exception);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public void GodotScriptPathAttributes_AreUnique()
    {
        var assemblyPath = FindFileInRepo(
            "game",
            ".godot",
            "mono",
            "temp",
            "bin",
            "Debug",
            "HarvestManor.dll");

        var loadContext = new ProjectAssemblyLoadContext(assemblyPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var scriptPaths = assembly.GetTypes()
                .Select(type => new
                {
                    Type = type.FullName,
                    Attribute = type.GetCustomAttributesData()
                        .FirstOrDefault(attribute => attribute.AttributeType.FullName == "Godot.ScriptPathAttribute")
                })
                .Where(entry => entry.Attribute is not null)
                .Select(entry => new
                {
                    entry.Type,
                    ScriptPath = entry.Attribute!.ConstructorArguments.Single().Value as string
                })
                .ToList();

            Assert.NotEmpty(scriptPaths);
            Assert.Equal(scriptPaths.Count, scriptPaths.Select(entry => entry.ScriptPath).Distinct(StringComparer.Ordinal).Count());
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static string FindFileInRepo(params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{Path.Combine(relativeSegments)}' from test output.");
    }

    private static string ExtractFirstMatch(string contents, string pattern, string groupName)
    {
        var match = Regex.Match(contents, pattern, RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"Pattern '{pattern}' was not found.");
        return match.Groups[groupName].Value;
    }

    private sealed class ProjectAssemblyLoadContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: true)
    {
        private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

        protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName assemblyName)
        {
            var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return resolvedPath is null ? null : LoadFromAssemblyPath(resolvedPath);
        }
    }
}
