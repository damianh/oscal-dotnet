// Copyright (c) Damian Hickey. All rights reserved.
// See LICENSE in the project root for license information.

using Metaschema;
using Metaschema.Constraints;
using Metaschema.Loading;
using Metaschema.Validation;

// Load the OSCAL Catalog Metaschema
var loader = new ModuleLoader();
var metaschemaPath = Path.Combine(AppContext.BaseDirectory, "Metaschema", "oscal_catalog_metaschema.xml");
var module = loader.Load(metaschemaPath);

Console.WriteLine($"Loaded metaschema: {module.Name}");
Console.WriteLine();

// Load the catalog content
var context = new BindingContext();
context.RegisterModule(module);

var catalogPath = Path.Combine(AppContext.BaseDirectory, "Content", "NIST_SP-800-53_rev5_catalog.json");
var deserializer = context.GetDeserializer(Format.Json);
var document = deserializer.Deserialize(File.ReadAllText(catalogPath));

Console.WriteLine("Loaded NIST SP 800-53 Rev 5 Catalog");
Console.WriteLine();

// Collect constraints from the module
var allConstraints = new List<IConstraint>();
var constraintCounts = new Dictionary<string, int>
{
    ["allowed-values"] = 0,
    ["matches"] = 0,
    ["expect"] = 0,
    ["index"] = 0,
    ["index-has-key"] = 0,
    ["is-unique"] = 0,
    ["has-cardinality"] = 0
};

foreach (var assembly in module.AssemblyDefinitions)
{
    allConstraints.AddRange(assembly.Constraints);
    CountConstraints(assembly.Constraints, constraintCounts);
}
foreach (var field in module.FieldDefinitions)
{
    allConstraints.AddRange(field.Constraints);
    CountConstraints(field.Constraints, constraintCounts);
}
foreach (var flag in module.FlagDefinitions)
{
    allConstraints.AddRange(flag.Constraints);
    CountConstraints(flag.Constraints, constraintCounts);
}

Console.WriteLine("Constraint types in catalog metaschema:");
foreach (var (type, count) in constraintCounts.Where(c => c.Value > 0))
{
    Console.WriteLine($"  {type}: {count}");
}

Console.WriteLine($"Total constraints: {allConstraints.Count}");
Console.WriteLine();

// Validate the document
var validator = new ConstraintValidator();
var adapter = new DocumentNodeAdapter(document);
var results = validator.ValidateAll(adapter, allConstraints);

Console.WriteLine($"Validation completed");
Console.WriteLine($"  Is valid: {results.IsValid}");
Console.WriteLine($"  Total findings: {results.Count}");
Console.WriteLine();

if (!results.IsValid || results.Count > 0)
{
    Console.WriteLine($"  Critical: {results.CriticalCount}");
    Console.WriteLine($"  Errors: {results.ErrorCount}");
    Console.WriteLine($"  Warnings: {results.WarningCount}");
    Console.WriteLine($"  Informational: {results.InformationalCount}");

    var sampleFindings = results.Findings.Take(5).ToList();
    if (sampleFindings.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Sample findings:");
        foreach (var finding in sampleFindings)
        {
            Console.WriteLine($"  [{finding.Severity}] {finding.Message}");
            if (finding.Location is not null)
            {
                Console.WriteLine($"           at: {finding.Location}");
            }
        }
        if (results.Count > 5)
        {
            Console.WriteLine($"  ... and {results.Count - 5} more findings");
        }
    }
}
else
{
    Console.WriteLine("No validation issues found - document is fully compliant!");
}

void CountConstraints(IReadOnlyList<IConstraint> constraints, Dictionary<string, int> counts)
{
    foreach (var constraint in constraints)
    {
        var key = constraint switch
        {
            IAllowedValuesConstraint => "allowed-values",
            IMatchesConstraint => "matches",
            IExpectConstraint => "expect",
            IIndexConstraint => "index",
            IIndexHasKeyConstraint => "index-has-key",
            IUniqueConstraint => "is-unique",
            ICardinalityConstraint => "has-cardinality",
            _ => null
        };
        if (key is not null)
        {
            counts[key]++;
        }
    }
}
