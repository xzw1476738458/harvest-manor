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
    public void ProjectGodot_ConfiguresWidescreenCanvasStretchForPresentation()
    {
        var projectContents = File.ReadAllText(FindFileInRepo("game", "project.godot"));

        Assert.Equal(
            "1280",
            ExtractFirstMatch(
                projectContents,
                "window/size/viewport_width=(?<value>\\d+)",
                "value"));

        Assert.Equal(
            "720",
            ExtractFirstMatch(
                projectContents,
                "window/size/viewport_height=(?<value>\\d+)",
                "value"));

        Assert.Equal(
            "canvas_items",
            ExtractFirstMatch(
                projectContents,
                "window/stretch/mode=\"(?<value>[^\"]+)\"",
                "value"));

        Assert.Equal(
            "keep",
            ExtractFirstMatch(
                projectContents,
                "window/stretch/aspect=\"(?<value>[^\"]+)\"",
                "value"));
    }

    [Fact]
    public void BuiltGameAssembly_ContainsGameBootstrapType()
    {
        var assemblyPath = ResolveBuiltGameAssemblyPath();

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
        var assemblyPath = ResolveBuiltGameAssemblyPath();

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
        var assemblyPath = ResolveBuiltGameAssemblyPath();

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
        var assemblyPath = ResolveBuiltGameAssemblyPath();

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
        if (TryFindFileInRepo(relativeSegments, out var path))
        {
            return path!;
        }

        throw new FileNotFoundException($"Could not locate '{Path.Combine(relativeSegments)}' from test output.");
    }

    private static bool TryFindFileInRepo(string[] relativeSegments, out string? path)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }

            directory = directory.Parent;
        }

        path = null;
        return false;
    }

    private static string ResolveBuiltGameAssemblyPath()
    {
        // The game DLL lives in different folders depending on who built it:
        //   - Godot editor build  -> game/.godot/mono/temp/bin/<Config>/HarvestManor.dll
        //   - `dotnet build` (CI) -> game/bin/<Config>/net8.0/HarvestManor.dll
        // Try every known location so these tests work both locally (after
        // launching Godot once) and on CI (where Godot is unavailable).
        string[][] candidates =
        {
            new[] { "game", ".godot", "mono", "temp", "bin", "Debug", "HarvestManor.dll" },
            new[] { "game", ".godot", "mono", "temp", "bin", "Release", "HarvestManor.dll" },
            new[] { "game", "bin", "Debug", "net8.0", "HarvestManor.dll" },
            new[] { "game", "bin", "Release", "net8.0", "HarvestManor.dll" },
        };

        foreach (var segments in candidates)
        {
            if (TryFindFileInRepo(segments, out var path))
            {
                return path!;
            }
        }

        throw new FileNotFoundException(
            "Could not locate the built HarvestManor.dll in any expected location. " +
            "Build the game project via the Godot editor or `dotnet build game/HarvestManor.csproj` before running these tests.");
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
