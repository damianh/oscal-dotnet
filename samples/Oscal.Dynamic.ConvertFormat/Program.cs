// Copyright (c) Damian Hickey. All rights reserved.
// See LICENSE in the project root for license information.

using Metaschema;
using Metaschema.Loading;

// Load the profile metaschema and document
var loader = new ModuleLoader();
var metaschemaPath = Path.Combine(AppContext.BaseDirectory, "Metaschema", "oscal_profile_metaschema.xml");
var module = loader.Load(metaschemaPath);

var context = new BindingContext();
context.RegisterModule(module);

var profilePath = Path.Combine(AppContext.BaseDirectory, "Content", "NIST_SP-800-53_rev5_LOW-baseline_profile.json");
var jsonContent = File.ReadAllText(profilePath);
var jsonDeserializer = context.GetDeserializer(Format.Json);
var document = jsonDeserializer.Deserialize(jsonContent);

Console.WriteLine("Loaded: NIST SP 800-53 LOW Baseline Profile (JSON)");
Console.WriteLine($"  Original size: {jsonContent.Length:N0} characters");
Console.WriteLine();

// Convert to XML
var xmlSerializer = context.GetSerializer(Format.Xml);
var xmlContent = xmlSerializer.SerializeToString(document);

Console.WriteLine("Converted to XML:");
Console.WriteLine($"  Size: {xmlContent.Length:N0} characters");
Console.WriteLine();
Console.WriteLine("  Preview:");
foreach (var line in xmlContent.Split('\n').Take(8))
{
    Console.WriteLine($"    {line.TrimEnd()}");
}

Console.WriteLine("    ...");
Console.WriteLine();

// Convert to YAML
var yamlSerializer = context.GetSerializer(Format.Yaml);
var yamlContent = yamlSerializer.SerializeToString(document);

Console.WriteLine("Converted to YAML:");
Console.WriteLine($"  Size: {yamlContent.Length:N0} characters");
Console.WriteLine();
Console.WriteLine("  Preview:");
foreach (var line in yamlContent.Split('\n').Take(12))
{
    Console.WriteLine($"    {line.TrimEnd()}");
}

Console.WriteLine("    ...");
Console.WriteLine();

// Round-trip back to JSON
var jsonSerializer = context.GetSerializer(Format.Json);
var roundTripJson = jsonSerializer.SerializeToString(document);

Console.WriteLine("Format Comparison:");
Console.WriteLine($"  JSON: {jsonContent.Length,10:N0} chars (100%)");
Console.WriteLine($"  XML:  {xmlContent.Length,10:N0} chars ({100.0 * xmlContent.Length / jsonContent.Length:F0}%)");
Console.WriteLine($"  YAML: {yamlContent.Length,10:N0} chars ({100.0 * yamlContent.Length / jsonContent.Length:F0}%)");
