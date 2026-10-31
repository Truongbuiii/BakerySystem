using BakerySystem.MobileApp.Services;
using Microsoft.Extensions.Logging;
#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
using Microsoft.Maui.DevFlow.Blazor;
#endif

namespace BakerySystem.MobileApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
#if MAUI_DEVFLOW
        builder.AddMauiDevFlowAgent();
        builder.AddMauiBlazorDevFlowTools();
#endif
        builder.Services.AddHttpClient<MobileApiClient>(client =>
        {
            client.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "ngrok-skip-browser-warning", "true");
        });
        builder.Services.AddScoped<CartService>();
        builder.Services.AddScoped<CustomerSessionService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

internal static class ApiConfiguration
{
    public static string BaseUrl
    {
        get
        {
#if WINDOWS
            return "http://localhost:5004/";
#else
            return "https://tandra-acetylenic-aurelio.ngrok-free.dev/";
#endif
        }
    }
}
