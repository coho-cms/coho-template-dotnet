// `dotnet run --project tools/Catalog`: every content type, and how many entries each has,
// read the way an app reads them. Run it from the repository's root.
//
// Released content by default, from delivery, with COHO_DELIVERY_URL and COHO_DELIVERY_KEY.
// The ref is `--ref`, or COHO_REF, or the ref in coho.config.json.
//
// `--preview` reads unreleased content instead, live through Coho, with the COHO_TOKEN this
// library pulls its types with, or COHO_PREVIEW_TOKEN if that is set. The ref is `--ref`, or
// `dev`.
//
// The project, and for a preview Coho's URL and the account, come from coho.config.json
// unless the environment or `.env` sets them, under the names an app's configuration uses.

using System.Text.Json.Nodes;
using Catalog;
using CohoCms.Delivery;

var preview = args.Contains("--preview");
var refIndex = Array.IndexOf(args, "--ref");
var askedRef = refIndex >= 0 && refIndex + 1 < args.Length ? args[refIndex + 1] : null;

var config = JsonNode.Parse(File.ReadAllText("coho.config.json"))!.AsObject();
string? Configured(string key) => (string?)config[key];

// The variables an app would set: the environment, then .env, with gaps filled from coho.config.json.
var env = new Dictionary<string, string?>(StringComparer.Ordinal);
foreach (var line in File.Exists(".env") ? File.ReadAllLines(".env") : [])
{
    var equals = line.IndexOf('=', StringComparison.Ordinal);
    if (equals > 0 && !line.TrimStart().StartsWith('#'))
    {
        env[line[..equals].Trim()] = line[(equals + 1)..].Trim().Trim('"');
    }
}
string? Get(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } set ? set : env.GetValueOrDefault(name);
void Fill(string name, string? value)
{
    if (string.IsNullOrWhiteSpace(Get(name)))
    {
        env[name] = value;
    }
}

Fill("COHO_PROJECT", Configured("project"));
var overrides = new Dictionary<string, string?>(StringComparer.Ordinal);
if (preview)
{
    Fill("COHO_PREVIEW_TOKEN", Get("COHO_TOKEN"));
    if (string.IsNullOrWhiteSpace(Get("COHO_PREVIEW_TOKEN")))
    {
        Console.Error.WriteLine("The catalog's --preview needs COHO_TOKEN in .env, a project token with read access. See .env.example.");
        return 1;
    }
    Fill("COHO_URL", Configured("url"));
    Fill("COHO_ACCOUNT", Configured("account"));
    overrides["COHO_DELIVERY_KEY"] = null;

    // Not the config's ref: that is a tag, and a preview reads branches.
    overrides["COHO_REF"] = askedRef ?? "dev";
}
else
{
    overrides["COHO_PREVIEW_TOKEN"] = null;
    Fill("COHO_REF", Configured("ref"));
    if (askedRef is not null)
    {
        overrides["COHO_REF"] = askedRef;
    }
}

ContentConfig read;
try
{
    read = CohoEnvironment.From(name => overrides.TryGetValue(name, out var value) ? value : Get(name));
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine($"The catalog: {exception.Message}. See .env.example.");
    return 1;
}

// Display names, from the type model the code was generated from.
var model = JsonNode.Parse(File.ReadAllText(Configured("typeModel") ?? "coho-ir.json"));

try
{
    var counted = await Catalogs.CountAsync(read);
    var rows = counted.Select(each => (each.Type, Name: (string?)model?["types"]?[each.Type]?["name"] ?? "", Count: each.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToList();
    var typeWidth = rows.Count == 0 ? 0 : rows.Max(row => row.Type.Length);
    var nameWidth = rows.Count == 0 ? 0 : rows.Max(row => row.Name.Length);
    var total = counted.Sum(each => each.Count);

    Console.WriteLine($"{(preview ? "Unreleased content" : "Content")} on {read.Ref}:\n");
    foreach (var (type, name, count) in rows)
    {
        Console.WriteLine($"  {type.PadRight(typeWidth)}   {name.PadRight(nameWidth)}   {count.PadLeft(5)}");
    }
    Console.WriteLine($"\n  {total} {(total == 1 ? "entry" : "entries")} in {counted.Count} {(counted.Count == 1 ? "type" : "types")}");
    return 0;
}
catch (CohoDeliveryException exception)
{
    // Coho's own words, such as KEY_REQUIRED or PROJECT_NOT_FOUND, and what to check.
    Console.Error.WriteLine(exception.Message);
    return 1;
}
