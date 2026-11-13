using BakerySystem.Application.Features.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace BakerySystem.WebShop.Services;

public sealed class RevalidatingAuthStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    public static readonly TimeSpan MaxSessionLifetime = TimeSpan.FromHours(4);

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(2);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return await SessionClaimHelper.ValidatePrincipalAsync(
            authenticationState.User,
            authService,
            MaxSessionLifetime,
            cancellationToken);
    }
}
