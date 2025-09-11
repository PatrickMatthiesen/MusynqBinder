using MusynqBinder.Data.Music;
using ConcertTracker.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using MusynqBinder.Shared.DTO;
using MusynqBinder.Shared.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisClient(connectionName: "cache");

builder.AddRedisOutputCache(connectionName: "cache");

builder.AddNpgsqlDbContext<MusicDbContext>(connectionName: "musynqbinder");

builder.Services.AddScoped<IConcertProvider, TicketmasterProvider>();
builder.Services.AddScoped<ConcertService>();

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd")); //todo 
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
app.UseOutputCache();

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

app.MapGet("/api/concerts/{artistName}", async (string artistName, ConcertService concertService) =>
{
    var concerts = await concertService.GetConcertsAsync(artistName);
    return concerts;
}).WithName("GetConcertsByArtist")
    .CacheOutput(policy => policy.Expire(TimeSpan.FromHours(24)))
    .WithOpenApi();

app.MapGet("/api/artists/name/like/{searchString}", async (string searchString, MusicDbContext context) =>
{
    var musicDbContext = context.Artists.Where(a => EF.Functions.ILike(a.Name, $"%{searchString}%"))
        .Select(a => a.Name);
    return await musicDbContext.ToListAsync();
}).WithName("GetArtistsWithNameLike")
    .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(20)))
    .WithOpenApi();

app.MapGet("/api/artists/{artistName}", async (string artistName, MusicDbContext context) =>
{
    var now = DateTimeOffset.UtcNow;
    var artist = await context.Artists
        .Include(a => a.Concerts.Where(c => c.Date > now))
        .Include(a => a.Sources)
        .FirstOrDefaultAsync(a => a.Name == artistName);

    return artist switch
    {
        null => Results.NotFound(),
        _ => Results.Ok(new ArtistDto
        {
            Id = artist.Id,
            Name = artist.Name,
            Concerts = [.. artist.Concerts.Select(c => new ConcertDto
            {
                Id = c.Id,
                Date = c.Date,
                VenueName = c.VenueName,
                City = c.City,
                Country = c.Country,
                Source = c.Source,
                SourceEventId = c.SourceEventId,
                TicketUrl = c.TicketUrl
            })],
            Sources = [.. artist.Sources.Select(s => new ArtistSourceDto
            {
                Id = s.Id,
                Source = s.Source,
                SourceArtistId = s.SourceArtistId,
                Url = s.Url
            })]
        })
    };
}).WithName("GetArtistById")
    .CacheOutput(policy => policy.Expire(TimeSpan.FromHours(24)))
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) {
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
