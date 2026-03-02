#:package Bullseye
#:package SimpleExec

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bullseye;
using static Bullseye.Targets;
using static SimpleExec.Command;

// Find repository root by looking for .git directory
var repoRoot = Directory.GetCurrentDirectory();
while (!Directory.Exists(Path.Combine(repoRoot, ".git")))
{
    repoRoot = Directory.GetParent(repoRoot) is { } parent
        ? parent.FullName
        : throw new InvalidOperationException("Could not find repository root (no .git directory found)");
}

// Parse custom arguments for update-oscal target
string? oscalVersion = null;
var remainingArgs = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    // If previous arg was "update-oscal", next arg is the version
    if (i > 0 && args[i - 1] == "update-oscal" && !args[i].StartsWith('-'))
    {
        oscalVersion = args[i];
    }
    else
    {
        remainingArgs.Add(args[i]);
    }
}

const string Clean = "clean";
const string DebugBuild = "debug-build";
const string Default = "default";
const string Pack = "pack";
const string ReleaseBuild = "release-build";
const string Restore = "restore";
const string Test = "test";
const string UpdateOscal = "update-oscal";
const string SolutionFile = "oscal-dotnet.slnx";

Target(Clean, () =>
    RunAsync("dotnet", $"clean {SolutionFile}", workingDirectory: repoRoot));

Target(Restore, () =>
    RunAsync("dotnet", $"restore {SolutionFile}", workingDirectory: repoRoot));

Target(DebugBuild, dependsOn: [Restore], () =>
    RunAsync("dotnet", $"build {SolutionFile} --no-restore -c Debug", workingDirectory: repoRoot));

Target(ReleaseBuild, dependsOn: [Restore], () =>
    RunAsync("dotnet", $"build {SolutionFile} --no-restore -c Release", workingDirectory: repoRoot));

Target(Test, dependsOn: [ReleaseBuild], async () =>
{
    await RunAsync("dotnet", "run --project test/Oscal.Tests --no-build -c Release", workingDirectory: repoRoot);
});

Target(Pack, dependsOn: [ReleaseBuild], () =>
    RunAsync("dotnet", $"pack {SolutionFile} --no-build -c Release -o artifacts/packages", workingDirectory: repoRoot));

Target(Default, [Clean, ReleaseBuild, Test]);

Target(UpdateOscal, async () =>
{
    if (string.IsNullOrEmpty(oscalVersion))
    {
        Console.WriteLine("Usage: dotnet run build.cs -- update-oscal <version|all>");
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run build.cs -- update-oscal 1.2.0");
        Console.WriteLine("  dotnet run build.cs -- update-oscal all");
        throw new InvalidOperationException("Version argument required");
    }

    var versions = await GetOscalVersionsToFetch(oscalVersion);

    foreach (var version in versions)
    {
        await FetchOscalVersion(version, repoRoot);
    }
});

await RunTargetsAndExitAsync([.. remainingArgs]);

// Helper methods

async Task<List<string>> GetOscalVersionsToFetch(string versionArg)
{
    if (!versionArg.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        // Validate semver format
        if (!Regex.IsMatch(versionArg, @"^\d+\.\d+\.\d+$"))
        {
            throw new InvalidOperationException($"Invalid version format: {versionArg}. Expected X.Y.Z");
        }
        return [versionArg];
    }

    // Fetch all non-prerelease tags from OSCAL repo
    Console.WriteLine("Fetching available OSCAL versions...");
    var (output, _) = await ReadAsync("git", "ls-remote --tags https://github.com/usnistgov/OSCAL.git");

    var releasePattern = new Regex(@"refs/tags/v(\d+\.\d+\.\d+)$");
    var versions = output
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => releasePattern.Match(line))
        .Where(m => m.Success)
        .Select(m => m.Groups[1].Value)
        .Distinct()
        .OrderByDescending(v => Version.Parse(v))
        .ToList();

    Console.WriteLine($"Found {versions.Count} release versions: {string.Join(", ", versions)}");
    return versions;
}

async Task FetchOscalVersion(string version, string repoRoot)
{
    var tag = $"v{version}";
    var repoUrl = "https://github.com/usnistgov/OSCAL.git";
    var referenceDir = Path.Combine(repoRoot, "reference", "oscal");
    var versionDir = Path.Combine(referenceDir, $"v{version}");
    var manifestPath = Path.Combine(referenceDir, "versions.json");
    var tempDir = Path.Combine(Path.GetTempPath(), $"oscal-fetch-{Guid.NewGuid()}");

    Console.WriteLine($"Fetching OSCAL {version}");

    // Remove existing version directory if present (always overwrite)
    if (Directory.Exists(versionDir))
    {
        Console.WriteLine($"Removing existing version at {versionDir}");
        Directory.Delete(versionDir, recursive: true);
    }

    // Ensure reference/oscal directory exists
    Directory.CreateDirectory(referenceDir);

    try
    {
        // Shallow clone at specific tag
        Console.WriteLine($"Cloning OSCAL repository (tag: {tag})...");
        await RunAsync("git", $"clone --depth 1 --branch {tag} {repoUrl} \"{tempDir}\"");

        // Verify src/metaschema exists
        var metaschemaSource = Path.Combine(tempDir, "src", "metaschema");
        if (!Directory.Exists(metaschemaSource))
            throw new InvalidOperationException($"Metaschema directory not found at {metaschemaSource}");

        // Copy metaschema files
        Console.WriteLine($"Copying metaschema files to {versionDir}...");
        CopyDirectory(metaschemaSource, versionDir);

        var fileCount = Directory.GetFiles(versionDir, "*", SearchOption.AllDirectories).Length;
        Console.WriteLine($"Copied {fileCount} files");

        // Update versions.json manifest
        UpdateVersionsManifest(manifestPath, version, tag);

        Console.WriteLine($"=== OSCAL {version} fetched successfully ===");
    }
    finally
    {
        // Cleanup temp directory
        if (Directory.Exists(tempDir))
        {
            Console.WriteLine("Cleaning up temporary files...");
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (UnauthorizedAccessException)
            {
                // Git may lock files on Windows; ignore cleanup errors
                Console.WriteLine($"Warning: Could not delete temp directory {tempDir} (Git files may be locked)");
            }
        }
    }
}

void CopyDirectory(string sourceDir, string destDir)
{
    Directory.CreateDirectory(destDir);

    foreach (var file in Directory.GetFiles(sourceDir))
    {
        var destFile = Path.Combine(destDir, Path.GetFileName(file));
        File.Copy(file, destFile, overwrite: true);
    }

    foreach (var dir in Directory.GetDirectories(sourceDir))
    {
        var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
        CopyDirectory(dir, destSubDir);
    }
}

void UpdateVersionsManifest(string manifestPath, string version, string tag)
{
    Console.WriteLine("Updating versions.json manifest...");

    List<JsonObject> versions = [];

    // Load existing manifest if present
    if (File.Exists(manifestPath))
    {
        var existingJson = File.ReadAllText(manifestPath);
        var doc = JsonNode.Parse(existingJson);
        if (doc?["versions"] is JsonArray arr)
        {
            versions = arr
                .OfType<JsonObject>()
                .Where(v => v["version"]?.ToString() != version)
                .Select(v => JsonNode.Parse(v.ToJsonString())!.AsObject()) // Clone to detach from parent
                .ToList();
        }
    }

    // Add new version entry
    var entry = new JsonObject
    {
        ["version"] = version,
        ["tag"] = tag,
        ["fetchedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture)
    };
    versions.Add(entry);

    // Sort by version descending
    versions = [.. versions.OrderByDescending(v => Version.Parse(v["version"]!.ToString()))];

    // Write manifest
    var manifest = new JsonObject
    {
        ["versions"] = new JsonArray([.. versions])
    };

    var options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(manifestPath, manifest.ToJsonString(options));
}
