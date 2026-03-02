// Copyright (c) Damian Hickey. All rights reserved.
// See LICENSE in the project root for license information.

using Metaschema;
using Metaschema.Loading;
using Metaschema.Nodes;

// Load the OSCAL Catalog Metaschema
var loader = new ModuleLoader();
var metaschemaPath = Path.Combine(AppContext.BaseDirectory, "Metaschema", "oscal_catalog_metaschema.xml");
var module = loader.Load(metaschemaPath);

Console.WriteLine($"Loaded metaschema: {module.Name} (version {module.Version})");
Console.WriteLine($"  Namespace: {module.XmlNamespace}");
Console.WriteLine($"  Assemblies: {module.AssemblyDefinitions.Count()}, Fields: {module.FieldDefinitions.Count()}");
Console.WriteLine();

// Create binding context and load the catalog JSON
var context = new BindingContext();
context.RegisterModule(module);

var catalogPath = Path.Combine(AppContext.BaseDirectory, "Content", "NIST_SP-800-53_rev5_catalog.json");
var deserializer = context.GetDeserializer(Format.Json);
var document = deserializer.Deserialize(File.ReadAllText(catalogPath));

var catalog = document.RootAssembly!;
Console.WriteLine($"Loaded catalog: {catalog.Name}");

// Get catalog UUID
if (catalog.Flags.TryGetValue("uuid", out var uuidFlag))
{
    Console.WriteLine($"  UUID: {uuidFlag.RawValue}");
}

// Extract metadata
var metadata = catalog.ModelChildren.FirstOrDefault(c => c.Name == "metadata") as AssemblyNode;
if (metadata is not null)
{
    var title = metadata.ModelChildren.FirstOrDefault(c => c.Name == "title") as FieldNode;
    var version = metadata.ModelChildren.FirstOrDefault(c => c.Name == "version") as FieldNode;
    var oscalVersion = metadata.ModelChildren.FirstOrDefault(c => c.Name == "oscal-version") as FieldNode;

    Console.WriteLine($"  Title: {title?.RawValue}");
    Console.WriteLine($"  Version: {version?.RawValue}");
    Console.WriteLine($"  OSCAL Version: {oscalVersion?.RawValue}");
}
Console.WriteLine();

// Count control families (groups)
var groups = catalog.ModelChildren.Where(c => c.Name == "group").Cast<AssemblyNode>().ToList();
Console.WriteLine($"Control Families: {groups.Count}");

foreach (var group in groups.Take(5))
{
    var groupId = group.Flags.TryGetValue("id", out var idFlag) ? idFlag.RawValue : "unknown";
    var groupTitle = group.ModelChildren.FirstOrDefault(c => c.Name == "title") as FieldNode;
    var controlCount = group.ModelChildren.Count(c => c.Name == "control");

    Console.WriteLine($"  [{groupId}] {groupTitle?.RawValue} ({controlCount} controls)");
}

if (groups.Count > 5)
{
    Console.WriteLine($"  ... and {groups.Count - 5} more families");
}

Console.WriteLine();

// Count total controls
int CountControls(AssemblyNode parent)
{
    var count = 0;
    foreach (var child in parent.ModelChildren.OfType<AssemblyNode>())
    {
        if (child.Name == "control")
        {
            count++;
            count += CountControls(child);
        }
        else if (child.Name == "group")
        {
            count += CountControls(child);
        }
    }
    return count;
}

Console.WriteLine($"Total controls (including enhancements): {CountControls(catalog)}");
