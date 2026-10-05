using System.Net;

namespace PersonalFinancialManagement.Tests.Support;

// Answers every request with a fixed response and keeps a copy of what was sent.
public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;

    public StubHttpMessageHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _body = body;
        _status = status;
    }

    public HttpRequestMessage? Request { get; private set; }

    public string? RequestBody { get; private set; }

    public string? RequestContentType { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        RequestContentType = request.Content?.Headers.ContentType?.ToString();
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
