using CohoCms.Delivery;

namespace YourOrg.Content;

/// <summary>
/// Reads of your own, built on the generated ones.
/// </summary>
/// <remarks>
/// <c>Coho/</c> is generated from the project's content types: after a schema change, run
/// <c>dotnet coho-codegen pull</c> and <c>dotnet coho-codegen generate</c>, and never edit it
/// by hand. Your reads belong here, beside it. Regeneration never touches this file, and a
/// schema change that removes something these reads use fails this library's build rather
/// than an app at runtime.
/// </remarks>
public static class CohoReads
{
    /// <summary>
    /// An example of a read of your own: the home page, and everything it links to two levels
    /// deep, in one request. Replace it with yours, or remove it.
    /// </summary>
    public static Task<EntryBase?> HomepageAsync(this Coho coho, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coho);
        return coho.BySlugAsync("home-page", new ReadOptions { Depth = 2 }, cancellationToken);
    }
}
