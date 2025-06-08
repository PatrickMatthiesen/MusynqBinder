using ConcertTracker.Api.Data;
using ConcertTracker.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using MusynqBinder.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

//builder.AddRedisOutputCache(connectionName: "cache");

builder.AddNpgsqlDbContext<AppDbContext>(connectionName: "musynqbinder");

builder.Services.AddScoped<IConcertProvider, TicketmasterProvider>();
builder.Services.AddScoped<ConcertService>();

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
//app.UseOutputCache();

var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
}).WithName("GetWeatherForecast")
    .WithOpenApi()
    .RequireAuthorization();
    //.CacheOutput();

app.MapGet("/", () =>
{
    return "Welcome to the concert service";
});

app.MapGet("/api/concerts/{artistName}", async (string artistName, HttpContext httpContext, ConcertService concertService) =>
{
    var concerts = await concertService.GetConcertsAsync(artistName);
    return concerts;
}).WithName("GetConcertsByArtist")
    //.CacheOutput()
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
