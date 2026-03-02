// Copyright (c) Damian Hickey. All rights reserved.
// See LICENSE in the project root for license information.

using System.Globalization;
using Metaschema;
using Metaschema.Loading;
using Metaschema.Metapath;
using Metaschema.Metapath.Context;
using Metaschema.Metapath.Item;
using Metaschema.Validation;

// Load the OSCAL Catalog
var loader = new ModuleLoader();
var metaschemaPath = Path.Combine(AppContext.BaseDirectory, "Metaschema", "oscal_catalog_metaschema.xml");
var module = loader.Load(metaschemaPath);

var context = new BindingContext();
context.RegisterModule(module);

var catalogPath = Path.Combine(AppContext.BaseDirectory, "Content", "NIST_SP-800-53_rev5_catalog.json");
var deserializer = context.GetDeserializer(Format.Json);
var document = deserializer.Deserialize(File.ReadAllText(catalogPath));

Console.WriteLine("Loaded NIST SP 800-53 Rev 5 Catalog");
Console.WriteLine();

var rootNode = new DocumentNodeAdapter(document);

// Simple path expressions
RunQuery("Catalog title", "catalog/metadata/title", rootNode);
RunQuery("OSCAL version", "catalog/metadata/oscal-version", rootNode);
RunQuery("Count control groups", "count(catalog/group)", rootNode);

// Filtering with predicates
RunQuery("Find control AC-1", "catalog//control[@id='ac-1']/title", rootNode);
RunQuery("Access Control family", "catalog/group[@id='ac']/title", rootNode);

// Aggregate functions
RunQuery("Count all controls", "count(catalog//control)", rootNode);
RunQuery("Count enhancements", "count(catalog//control/control)", rootNode);

// String functions
RunQuery("AC-1 exists?", "exists(catalog//control[@id='ac-1'])", rootNode);
RunQuery("Controls starting with 'ac-1'", "count(catalog//control[starts-with(@id, 'ac-1')])", rootNode);

// More complex queries
RunQuery("All group IDs", "catalog/group/@id", rootNode, maxResults: 5);
RunQuery("Controls with parameters", "count(catalog//control[param])", rootNode);

void RunQuery(string description, string expression, INodeItem contextNode, int maxResults = 1)
{
    Console.WriteLine($"Query: {description}");
    Console.WriteLine($"  Expression: {expression}");

    try
    {
        var expr = MetapathExpression.Compile(expression);
        var metapathContext = MetapathContext.Create().ForNode(contextNode);
        var result = expr.Evaluate(metapathContext);

        if (result.IsEmpty)
        {
            Console.WriteLine($"  Result: (empty)");
        }
        else if (result.Count == 1)
        {
            Console.WriteLine($"  Result: {FormatItem(result.FirstOrDefault)}");
        }
        else
        {
            Console.WriteLine($"  Results ({result.Count} items):");
            foreach (var item in result.Take(maxResults))
            {
                Console.WriteLine($"    - {FormatItem(item)}");
            }

            if (result.Count > maxResults)
            {
                Console.WriteLine($"    ... and {result.Count - maxResults} more");
            }
        }
    }
    catch (MetapathException ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
    }
    Console.WriteLine();
}

string FormatItem(IItem? item) => item switch
{
    null => "(null)",
    BooleanItem b => b.Value ? "true" : "false",
    IntegerItem i => i.Value.ToString(CultureInfo.InvariantCulture),
    DecimalItem d => d.Value.ToString(CultureInfo.InvariantCulture),
    StringItem s => $"\"{s.Value}\"",
    INodeItem node => node.GetStringValue(),
    _ => item.GetStringValue()
};
