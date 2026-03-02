// Copyright (c) Damian Hickey. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json;
using Metaschema.Loading;
using Metaschema.SchemaGeneration;
using Metaschema.SchemaGeneration.JsonSchema;
using Metaschema.SchemaGeneration.Xsd;

var loader = new ModuleLoader();

// Generate JSON Schema from Catalog Metaschema
var catalogMetaschemaPath = Path.Combine(AppContext.BaseDirectory, "Metaschema", "oscal_catalog_metaschema.xml");
var catalogModule = loader.Load(catalogMetaschemaPath);

var jsonSchemaOptions = new SchemaGenerationOptions { IncludeDocumentation = true };
var jsonSchemaGenerator = new JsonSchemaGenerator(jsonSchemaOptions);
var catalogJsonSchema = jsonSchemaGenerator.Generate(catalogModule);

var jsonSchemaText = FormatJsonDocument(catalogJsonSchema);
Console.WriteLine($"Generated JSON Schema for OSCAL Catalog: {jsonSchemaText.Length:N0} characters");
Console.WriteLine();
Console.WriteLine("JSON Schema preview:");
foreach (var line in jsonSchemaText.Split('\n').Take(15))
{
    Console.WriteLine($"  {line}");
}

Console.WriteLine("  ...");
Console.WriteLine();

// Generate XSD from Catalog Metaschema
var xsdGenerator = new XsdGenerator();
var catalogXsd = xsdGenerator.Generate(catalogModule);
var catalogXsdText = catalogXsd.ToString();

Console.WriteLine($"Generated XSD for OSCAL Catalog: {catalogXsdText.Length:N0} characters");
Console.WriteLine();
Console.WriteLine("XSD preview:");
foreach (var line in catalogXsdText.Split('\n').Take(12))
{
    Console.WriteLine($"  {line.TrimEnd()}");
}

Console.WriteLine("  ...");
Console.WriteLine();

// Generate schemas for all OSCAL models
var models = new[]
{
    ("oscal_catalog_metaschema.xml", "Catalog"),
    ("oscal_profile_metaschema.xml", "Profile"),
    ("oscal_ssp_metaschema.xml", "SSP"),
    ("oscal_component_metaschema.xml", "Component Definition"),
    ("oscal_assessment-plan_metaschema.xml", "Assessment Plan"),
    ("oscal_assessment-results_metaschema.xml", "Assessment Results"),
    ("oscal_poam_metaschema.xml", "POA&M")
};

Console.WriteLine("Schema sizes for all OSCAL models:");
Console.WriteLine("  Model                  | JSON Schema | XSD");
Console.WriteLine("  -----------------------|-------------|--------");

foreach (var (filename, displayName) in models)
{
    var metaschemaPath = Path.Combine(AppContext.BaseDirectory, "Metaschema", filename);
    if (!File.Exists(metaschemaPath))
    {
        Console.WriteLine($"  {displayName,-22} | (not found) | (not found)");
        continue;
    }

    try
    {
        var module = loader.Load(metaschemaPath);
        var jsonSchema = jsonSchemaGenerator.Generate(module);
        var jsonSchemaSize = FormatJsonDocument(jsonSchema).Length;
        var xsd = xsdGenerator.Generate(module);
        var xsdSize = xsd.ToString().Length;

        Console.WriteLine($"  {displayName,-22} | {jsonSchemaSize / 1024,7:N0} KB | {xsdSize / 1024,4:N0} KB");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {displayName,-22} | Error: {ex.Message}");
    }
}

string FormatJsonDocument(JsonDocument doc)
{
    using var stream = new MemoryStream();
    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
    doc.WriteTo(writer);
    writer.Flush();
    return System.Text.Encoding.UTF8.GetString(stream.ToArray());
}
