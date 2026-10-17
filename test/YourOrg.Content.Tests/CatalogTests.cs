using System.Globalization;
using System.Text.Json;
using Catalog;
using CohoCms.Delivery;

namespace YourOrg.Content.Tests;

public sealed class CatalogTests
{
    /// <summary>Holds for any project's types, so it keeps passing after you regenerate for yours.</summary>
    [Fact]
    public async Task The_catalog_counts_every_type_a_hundred_entries_a_request()
    {
        // Sizes either side of a page, so some types take more than one request.
        var types = Coho.Shape.Keys.Order(StringComparer.Ordinal).ToList();
        int[] pattern = [30, 100, 170, 0];
        var sizes = types.Select((type, i) => (type, size: pattern[i % 4])).ToDictionary(each => each.type, each => each.size);

        var recorder = new Recorder(uri =>
        {
            var total = sizes.GetValueOrDefault(Recorder.Query(uri, "type"));
            var offset = int.Parse(Recorder.Query(uri, "offset"), CultureInfo.InvariantCulture);
            var size = Math.Max(0, Math.Min(int.Parse(Recorder.Query(uri, "limit"), CultureInfo.InvariantCulture), total - offset));
            var roots = Enumerable.Range(offset, size).Select(i => $"entry-{i}");
            return JsonSerializer.Serialize(new { roots, entries = new { }, unresolved = new { }, hasMore = offset + size < total });
        });

        var counted = await Catalogs.CountAsync(
            new ContentConfig { Delivery = new ReleasedConfig { Url = "https://delivery.test", Project = "p1" }, Ref = "qa" },
            new ClientOptions { HttpClient = new HttpClient(recorder) },
            TestContext.Current.CancellationToken);

        Assert.Equal(types.Select(type => new CatalogEntry(type, sizes[type])), counted);

        // One request per hundred entries, and one for an empty type: 170 asks at offsets 0 and 100.
        static int Pages(int size) => Math.Max(1, (size + 99) / 100);
        Assert.Equal(
            types.SelectMany(type => Enumerable.Range(0, Pages(sizes[type])).Select(page => $"{type}@{page * 100}")),
            recorder.Asked.Select(url => new Uri(url)).Select(uri => $"{Recorder.Query(uri, "type")}@{Recorder.Query(uri, "offset")}"));
    }
}
