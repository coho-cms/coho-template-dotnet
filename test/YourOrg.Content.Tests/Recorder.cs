using System.Text;
using System.Web;

namespace YourOrg.Content.Tests;

/// <summary>A handler that records each URL it is asked for, and answers with <paramref name="answer"/>.</summary>
internal sealed class Recorder(Func<Uri, string> answer) : HttpMessageHandler
{
    public List<string> Asked { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Asked.Add(request.RequestUri!.AbsoluteUri);
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(answer(request.RequestUri), Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }

    public static string Query(Uri uri, string name) => HttpUtility.ParseQueryString(uri.Query)[name] ?? "";
}
