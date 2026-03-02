# oscal-dotnet

[![CI](https://github.com/damianh/oscal-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/damianh/oscal-dotnet/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub Stars](https://img.shields.io/github/stars/damianh/oscal-dotnet.svg)](https://github.com/damianh/oscal-dotnet/stargazers)

Strongly-typed C# models for [OSCAL](https://pages.nist.gov/OSCAL/) (Open Security Controls Assessment Language), generated from [NIST Metaschema](https://pages.nist.gov/metaschema/) definitions.

## Packages

| Package | Description | NuGet | Downloads |
|---------|-------------|-------|-----------|
| **DamianH.Oscal** | Strongly-typed C# models for OSCAL generated from NIST Metaschema definitions | [![NuGet](https://img.shields.io/nuget/v/DamianH.Oscal.svg)](https://www.nuget.org/packages/DamianH.Oscal/) | [![Downloads](https://img.shields.io/nuget/dt/DamianH.Oscal.svg)](https://www.nuget.org/packages/DamianH.Oscal/) |

## Features

- **All 8 OSCAL model types** &mdash; Catalog, Profile, Component Definition, SSP, SAP, SAR, POA&M, and Mapping
- **Versioned namespaces** &mdash; `Oscal.V1_2_0`, allowing multiple OSCAL versions to coexist
- **Zero runtime dependencies** &mdash; pure models with `System.Text.Json` source generation
- **Modern C#** &mdash; `sealed record` types, `required` properties, `init`-only setters, `IReadOnlyList<T>` collections
- **High-performance serialization** &mdash; JSON source generation via `V1_2_0JsonContext`

## Installation

```bash
dotnet add package DamianH.Oscal
```

> Requires .NET 10.0 or later.

## Quick Start

```csharp
using System.Text.Json;
using Oscal.V1_2_0;

// Deserialize using source-generated context (fastest)
var json = File.ReadAllText("nist-800-53-catalog.json");
var catalog = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.Catalog);

Console.WriteLine($"Catalog: {catalog.Metadata.Title}");

// Serialize back to JSON
var output = JsonSerializer.Serialize(catalog, V1_2_0JsonContext.Default.Catalog);
```

### All OSCAL Model Types

```csharp
using System.Text.Json;
using Oscal.V1_2_0;

var catalog   = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.Catalog);
var profile   = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.Profile);
var ssp       = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.SystemSecurityPlan);
var compDef   = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.ComponentDefinition);
var sap       = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.AssessmentPlan);
var sar       = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.AssessmentResults);
var poam      = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.PlanOfActionAndMilestones);
var mapping   = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.MappingCollection);
```

### Working with Multiple OSCAL Versions

```csharp
using OscalV1_2_0 = Oscal.V1_2_0;
using OscalV1_3_0 = Oscal.V1_3_0; // future

var oldCatalog = JsonSerializer.Deserialize<OscalV1_2_0.Catalog>(oldJson);
var newCatalog = JsonSerializer.Deserialize<OscalV1_3_0.Catalog>(newJson);
```

## OSCAL Versions

| OSCAL Version | Namespace | Status |
|---------------|-----------|--------|
| 1.2.0 | `Oscal.V1_2_0` | Generated |

Reference metaschema definitions are stored for all OSCAL releases from v1.0.0 through v1.2.0.

## Generated Code

Models are generated from [NIST OSCAL Metaschema](https://github.com/usnistgov/OSCAL) XML definitions. Each type is a `sealed record`:

```csharp
public sealed record Catalog
{
    [JsonPropertyName("uuid")]
    public required Guid Uuid { get; init; }

    [JsonPropertyName("metadata")]
    public required Metadata Metadata { get; init; }

    [JsonPropertyName("controls")]
    public IReadOnlyList<Control> Controls { get; init; } = [];

    [JsonPropertyName("groups")]
    public IReadOnlyList<Group> Groups { get; init; } = [];

    [JsonPropertyName("back-matter")]
    public BackMatter? BackMatter { get; init; }
}
```

JSON serialization uses `System.Text.Json` source generation with kebab-case naming:

```csharp
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Catalog))]
[JsonSerializable(typeof(Profile))]
// ... all 134 types
public partial class V1_2_0JsonContext : JsonSerializerContext { }
```

## Building

```bash
dotnet run build.cs                    # clean + build + test
dotnet run build.cs -- pack            # create NuGet packages
dotnet run build.cs -- test            # run tests only
```

### Updating OSCAL Metaschema Definitions

```bash
dotnet run build.cs -- update-oscal 1.2.0   # fetch specific version
dotnet run build.cs -- update-oscal all     # fetch all versions
```

## References

- [OSCAL Project (NIST)](https://pages.nist.gov/OSCAL/)
- [OSCAL GitHub](https://github.com/usnistgov/OSCAL)
- [Metaschema Specification](https://pages.nist.gov/metaschema/)
- [NIST SP 800-53 Rev. 5](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)

## License

MIT
