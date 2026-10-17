using System.Text.RegularExpressions;
using CohoCms.Delivery;

namespace YourOrg.Content.Tests;

/// <summary>
/// Checks that hold for any project's types, so they keep passing after you regenerate for
/// yours. Add tests for your own reads beside them.
/// </summary>
public sealed partial class LibraryTests
{
    [Fact]
    public void The_library_names_the_type_model_it_was_generated_from()
    {
        Assert.Matches(SchemaHash(), Coho.Source.SchemaHash);
    }

    [Fact]
    public async Task A_client_asks_nothing_until_it_is_read_from_and_the_home_page_is_one_request()
    {
        var recorder = new Recorder(_ => """{ "roots": [], "entries": {}, "unresolved": {} }""");
        var coho = new Coho(
            new ContentConfig { Delivery = new ReleasedConfig { Url = "https://delivery.test", Project = Coho.Source.Project }, Ref = "prod" },
            new ClientOptions { HttpClient = new HttpClient(recorder) });
        Assert.Empty(recorder.Asked);

        Assert.Null(await coho.HomepageAsync(TestContext.Current.CancellationToken));
        Assert.Equal([$"https://delivery.test/v1/projects/{Uri.EscapeDataString(Coho.Source.Project)}/prod/entries-by-slug/home-page?depth=2"], recorder.Asked);
    }

    [GeneratedRegex("^sha256:[0-9a-f]{64}$")]
    private static partial Regex SchemaHash();
}
