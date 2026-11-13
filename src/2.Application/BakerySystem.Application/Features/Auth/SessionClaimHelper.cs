using System.Globalization;
using System.Security.Claims;

namespace BakerySystem.Application.Features.Auth;

public static class SessionClaimHelper
{
    public const string SessionStampClaim = "SessionStamp";
    public const string AuthIssuedUtcClaim = "AuthIssuedUtc";

    public static List<Claim> BuildSessionClaims(
        int accountId,
        string username,
        string role,
        string? fullName,
        string? sessionStamp)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("FullName", string.IsNullOrWhiteSpace(fullName) ? username : fullName),
            new(AuthIssuedUtcClaim, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture))
        };

        if (!string.IsNullOrWhiteSpace(sessionStamp))
        {
            claims.Add(new Claim(SessionStampClaim, sessionStamp));
        }

        return claims;
    }

    public static async Task<bool> ValidatePrincipalAsync(
        ClaimsPrincipal? principal,
        IAuthService authService,
        TimeSpan maxSessionLifetime,
        CancellationToken ct = default)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var idValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;
        var stamp = principal.FindFirst(SessionStampClaim)?.Value;
        var issuedValue = principal.FindFirst(AuthIssuedUtcClaim)?.Value;

        if (!int.TryParse(idValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var accountId))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(issuedValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var issuedUtc))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - issuedUtc > maxSessionLifetime)
        {
            return false;
        }

        return await authService.IsSessionValidAsync(accountId, role, stamp, ct);
    }
}
