using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BakerySystem.Application.Features.Auth;
using BakerySystem.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace BakerySystem.API;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "BakerySystem.API";
    public string Audience { get; set; } = "BakerySystem.MobileApp";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiresHours { get; set; } = 24;
}

public sealed class JwtTokenService
{
    public const string CustomerIdClaim = "customer_id";

    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
        if (string.IsNullOrWhiteSpace(settings.SigningKey) || Encoding.UTF8.GetByteCount(settings.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 bytes long.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
    }

    public TimeSpan TokenLifetime => TimeSpan.FromHours(Math.Max(1, _settings.ExpiresHours));

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _settings.Issuer,
        ValidateAudience = true,
        ValidAudience = _settings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };

    public string CreateCustomerToken(CustomerDto customer, string? sessionStamp)
    {
        if (customer.AccountId is null or <= 0)
        {
            throw new InvalidOperationException("Customer account is required to issue an access token.");
        }

        var claims = SessionClaimHelper.BuildSessionClaims(
            customer.AccountId.Value,
            customer.Username,
            AccountRole.Customer,
            customer.FullName,
            sessionStamp);

        claims.Add(new Claim(CustomerIdClaim, customer.CustomerId.ToString(CultureInfo.InvariantCulture)));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
