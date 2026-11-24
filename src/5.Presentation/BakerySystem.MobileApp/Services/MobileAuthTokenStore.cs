using System.Net.Http.Headers;

namespace BakerySystem.MobileApp.Services;

public sealed class MobileTokenStore
{
    private readonly object _gate = new();
    private string? _accessToken;

    public string? AccessToken
    {
        get
        {
            lock (_gate)
            {
                return _accessToken;
            }
        }
    }

    public void SetToken(string? token)
    {
        lock (_gate)
        {
            _accessToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        }
    }

    public void Clear() => SetToken(null);
}

public sealed class MobileAuthHeaderHandler(MobileTokenStore tokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = tokenStore.AccessToken;
        if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
