using CohoCms.Delivery;

namespace Catalog;

/// <summary>How many entries one content type has.</summary>
/// <param name="Type">The type's slug.</param>
public sealed record CatalogEntry(string Type, int Count);

public static class Catalogs
{
    /// <summary>
    /// Every content type this library was generated for, with how many entries each has on
    /// the config's ref.
    /// </summary>
    /// <remarks>
    /// The types come from the generated <see cref="Coho.Shape"/>, and each is counted by
    /// paging through its list, a hundred entries a request, so it is for looking around,
    /// not for every page view.
    /// </remarks>
    public static async Task<IReadOnlyList<CatalogEntry>> CountAsync(ContentConfig config, ClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var delivery = new DeliveryClient(config.Delivery, (options ?? new ClientOptions()) with { MaxCachedResponses = 0 });
        var counted = new List<CatalogEntry>();

        foreach (var type in Coho.Shape.Keys.Order(StringComparer.Ordinal))
        {
            var count = 0;
            var more = true;
            while (more)
            {
                var page = await delivery.EntriesAsync(config.Ref, new ListOptions { Type = type, Limit = 100, Offset = count }, cancellationToken).ConfigureAwait(false);
                count += page.Roots.Length;
                more = page.HasMore && page.Roots.Length > 0;
            }
            counted.Add(new CatalogEntry(type, count));
        }
        return counted;
    }
}
