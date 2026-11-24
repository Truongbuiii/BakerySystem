using System.Globalization;
using System.Text.Json.Serialization;
using BakerySystem.Application;
using BakerySystem.Application.Features.Auth;
using BakerySystem.API;
using BakerySystem.Domain.Enums;
using BakerySystem.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Register Clean Architecture Infrastructure Services (EF Core DbContext)
builder.Services.AddInfrastructureServices(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var jwtTokenService = new JwtTokenService(jwtSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(jwtTokenService);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = jwtTokenService.CreateValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var customerClaim = context.Principal?.FindFirst(JwtTokenService.CustomerIdClaim)?.Value;
                if (!int.TryParse(customerClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var customerId)
                    || customerId <= 0)
                {
                    context.Fail("Token missing valid customer_id claim.");
                    return;
                }

                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var isValid = await SessionClaimHelper.ValidatePrincipalAsync(
                    context.Principal,
                    authService,
                    jwtTokenService.TokenLifetime,
                    context.HttpContext.RequestAborted);

                if (!isValid)
                {
                    context.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
                }
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("MobileCustomer", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(AccountRole.Customer));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}
app.UseCors("MobileApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapMobileApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
