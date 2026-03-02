# OSCAL Library

This library contains strongly-typed C# models for OSCAL (Open Security Controls Assessment Language), generated from NIST Metaschema definitions.

## Installation

Install via NuGet:

```bash
dotnet add package DamianH.Oscal
```

Or via Package Manager Console:

```powershell
Install-Package DamianH.Oscal
```

## Versioning Strategy

The OSCAL library uses a **versioned namespace approach** to allow multiple OSCAL versions to coexist in the same application:

- Each OSCAL version is in its own namespace: `Oscal.V1_2_0`, `Oscal.V1_3_0`, etc.
- Each version's code is in a separate folder: `V1_2_0/Generated/`, `V1_3_0/Generated/`, etc.
- This prevents both file clashes and type clashes between versions
- Applications can reference multiple versions simultaneously if needed

### Current Versions

| OSCAL Version | Namespace | Generated From |
|---------------|-----------|----------------|
| 1.2.0 | `Oscal.V1_2_0` | `oscal-metaschema-v1.2.0/oscal_complete_metaschema.xml` |

## Usage

### Basic JSON Deserialization

```csharp
using System.Text.Json;
using Oscal.V1_2_0;

// Load an OSCAL catalog from JSON
var json = File.ReadAllText("nist-800-53-catalog.json");
var catalog = JsonSerializer.Deserialize<Catalog>(json, V1_2_0JsonContext.Default.Options);

Console.WriteLine($"Catalog: {catalog.Metadata.Title}");
Console.WriteLine($"Controls: {catalog.Groups.SelectMany(g => g.Controls).Count()}");
```

### Using System.Text.Json Source Generation

The library includes source-generated JSON contexts for high-performance serialization:

```csharp
using System.Text.Json;
using Oscal.V1_2_0;

// Deserialize using source-generated context
var catalog = JsonSerializer.Deserialize(json, V1_2_0JsonContext.Default.Catalog);

// Serialize back to JSON
var outputJson = JsonSerializer.Serialize(catalog, V1_2_0JsonContext.Default.Catalog);
```

### Using a Specific Version

```csharp
using Oscal.V1_2_0;

// The namespace tells you the version: Oscal.V1_2_0 = OSCAL v1.2.0
var catalog = JsonSerializer.Deserialize<Catalog>(json, V1_2_0JsonContext.Default.Options);
Console.WriteLine($"Loaded catalog: {catalog.Metadata.Title}");
```

### Working with Different OSCAL Model Types

```csharp
using Oscal.V1_2_0;

// Load a catalog (controls library)
var catalog = JsonSerializer.Deserialize<Catalog>(catalogJson, V1_2_0JsonContext.Default.Catalog);

// Load a profile (control baseline)
var profile = JsonSerializer.Deserialize<Profile>(profileJson, V1_2_0JsonContext.Default.Profile);

// Load a system security plan
var ssp = JsonSerializer.Deserialize<SystemSecurityPlan>(sspJson, V1_2_0JsonContext.Default.SystemSecurityPlan);

// Load a POA&M (plan of action and milestones)
var poam = JsonSerializer.Deserialize<PlanOfActionAndMilestones>(poamJson, V1_2_0JsonContext.Default.PlanOfActionAndMilestones);
```

### Working with Multiple Versions

```csharp
using OscalV1_2_0 = Oscal.V1_2_0;
using OscalV1_3_0 = Oscal.V1_3_0;

// Load documents from different OSCAL versions
var oldCatalog = JsonSerializer.Deserialize<OscalV1_2_0.Catalog>(oldJson);
var newCatalog = JsonSerializer.Deserialize<OscalV1_3_0.Catalog>(newJson);

// Migrate between versions
var migratedCatalog = MigrateCatalog(oldCatalog);
```

## For Library Developers

### Regenerating Code

To regenerate the C# models from updated metaschema definitions:

```powershell
.\generate-oscal.ps1
```

Or manually:

```bash
dotnet run --project ../../src/Metaschema.Tool -- \
  generate-code \
  ../../reference/oscal/v1.2.0/oscal_complete_metaschema.xml \
  --namespace Oscal.V1_2_0 \
  --output V1_2_0/Generated
```

### Adding a New OSCAL Version

1. Fetch the new OSCAL version metaschema files:
   ```bash
   dotnet run build.cs -- update-oscal 1.3.0
   ```

2. Generate code for the new version:
   ```bash
   dotnet run --project src/Metaschema.Tool -- \
     generate-code \
     reference/oscal/v1.3.0/oscal_complete_metaschema.xml \
     --namespace Oscal.V1_3_0 \
     --output src/Oscal/V1_3_0/Generated
   ```

3. Update `Oscal.csproj` to include the new folder:
   ```xml
   <Compile Include="V1_3_0\Generated\**\*.g.cs" />
   ```

## Project Structure

```
Oscal/
├── Oscal.csproj                    # Project file with version includes
├── README.md                       # This file
├── generate-oscal.ps1              # Code generation script
├── V1_2_0/                         # OSCAL v1.2.0
│   └── Generated/
│       ├── Catalog.g.cs           # namespace Oscal.V1_2_0
│       ├── Profile.g.cs
│       ├── SystemSecurityPlan.g.cs
│       ├── V1_2_0JsonContext.g.cs # System.Text.Json context
│       └── ... (134 total files)
└── V1_3_0/                         # Future: OSCAL v1.3.0
    └── Generated/
        └── ...
```

## OSCAL Models

All 8 OSCAL models are included in each version:

| Model | Root Type | Purpose |
|-------|-----------|---------|
| Catalog | `Catalog` | Control catalogs (e.g., NIST SP 800-53) |
| Profile | `Profile` | Control baselines (e.g., FedRAMP High) |
| Component Definition | `ComponentDefinition` | Component descriptions and control implementations |
| System Security Plan (SSP) | `SystemSecurityPlan` | System security plans |
| Assessment Plan (SAP) | `AssessmentPlan` | Security assessment plans |
| Assessment Results (SAR) | `AssessmentResults` | Security assessment results |
| Plan of Action & Milestones (POA&M) | `PlanOfActionAndMilestones` | Remediation plans |
| Mapping | `MappingCollection` | Control mappings between frameworks |

## Generated Code Features

- **Modern C# records** with `required` properties and init-only setters
- **System.Text.Json source generation** for high-performance serialization
- **De-duplicated shared types** (Metadata, Property, Link, BackMatter, etc.)
- **Comprehensive XML documentation** from metaschema descriptions
- **Type-safe models** validated against official NIST metaschema definitions

## Requirements

- **.NET 10.0** or later

## License

MIT License. See LICENSE file in the repository.

## Source Metaschemas

The metaschema sources are stored in the repository under `reference/oscal/`:

```
reference/oscal/
├── v1.2.0/
│   ├── oscal_complete_metaschema.xml
│   ├── oscal_catalog_metaschema.xml
│   ├── oscal_profile_metaschema.xml
│   ├── oscal_ssp_metaschema.xml
│   ├── oscal_assessment-plan_metaschema.xml
│   ├── oscal_assessment-results_metaschema.xml
│   ├── oscal_poam_metaschema.xml
│   ├── oscal_component_metaschema.xml
│   ├── oscal_mapping_metaschema.xml
│   └── ... (common/shared modules)
├── v1.3.0/                         # Future versions
│   └── ...
└── versions.json                   # Version manifest
```

## References

- **OSCAL Project**: https://pages.nist.gov/OSCAL/
- **OSCAL GitHub**: https://github.com/usnistgov/OSCAL
- **Metaschema Specification**: https://pages.nist.gov/metaschema/
- **NIST SP 800-53**: https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final
