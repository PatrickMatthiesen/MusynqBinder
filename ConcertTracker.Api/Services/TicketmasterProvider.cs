using MusynqBinder.Data.Music;
using Microsoft.EntityFrameworkCore;
using MusynqBinder.Shared.Models;
using System.Linq;
using System.Text.Json;

namespace ConcertTracker.Api.Services;

public class TicketmasterProvider(MusicDbContext context, HttpClient httpClient, IConfiguration configuration, ILogger<TicketmasterProvider> logger) : IConcertProvider
{
    private readonly string _apiKey = configuration["Ticketmaster:ApiKey"] ?? throw new InvalidOperationException("Ticketmaster API key not configured.");


    public async Task<IEnumerable<Concert>> FetchConcertsAsync(string artistName)
    {
        var cacheExpiryHours = 24;
        var cutoff = DateTime.UtcNow.AddHours(-cacheExpiryHours);
    
        var existing = await context.Artists
            .AsNoTracking()
            .Where(a => EF.Functions.ILike(a.Name, $"{artistName}%"))
            .Include(a => a.Concerts.Where(c => c.Date > DateTime.UtcNow))
            .ThenInclude(c => c.Artists)
            .ToListAsync();

        if (existing.Count != 0 && existing.All(a => a.LastUpdated > cutoff))
        {
            logger.LogInformation("Artist {ArtistName} already exists in the database.", artistName);
            return existing.SelectMany(a => a.Concerts);
        }

        logger.LogInformation("Fetching concerts for artist: {ArtistName}", artistName);
        var attractionId = await GetAttractionIdAsync(artistName);
        if (string.IsNullOrEmpty(attractionId))
            return [];

        logger.LogInformation("Found attraction ID: {AttractionId} for artist: {ArtistName}", attractionId, artistName);

        var concerts = await GetEventsAsync(attractionId);

        // could probably be done after the result has been streamed to the client
        await context.Artists
            .Where(a => EF.Functions.ILike(a.Name, artistName))
            .ExecuteUpdateAsync(a => a.SetProperty(p => p.LastUpdated, DateTimeOffset.UtcNow));

        return concerts;
    }

    private async Task<string?> GetAttractionIdAsync(string artistName)
    {
        var url = $"https://app.ticketmaster.com/discovery/v2/attractions.json?apikey={_apiKey}&keyword={Uri.EscapeDataString(artistName)}";
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        if (json == null || !json.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("attractions", out var attractions))
            return null;

        foreach (var attraction in attractions.EnumerateArray())
        {
            // For now we just get any concert with the artist
            // Needs to be updated to return all the artists, so we dont miss any concerts
            var name = attraction.GetProperty("name").GetString();
            if (string.Equals(name, artistName, StringComparison.OrdinalIgnoreCase))
                return attraction.GetProperty("id").GetString();
        }

        return null;
    }

    private async Task<IEnumerable<Concert>> GetEventsAsync(string attractionId)
    {
        var url = $"https://app.ticketmaster.com/discovery/v2/events.json?apikey={_apiKey}&attractionId={attractionId}&size=100";
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        if (json == null || !json.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            return [];

        // Collect all unique artist names from all events first
        var allArtistNames = events.EnumerateArray()
            .SelectMany(ev => ev.GetProperty("_embedded").GetProperty("attractions")
                .EnumerateArray()
                .Select(a => a.GetProperty("name").GetString() ?? string.Empty))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Load existing artists from database in one query
        var existingArtists = await context.Artists
            .AsNoTracking()
            .Where(a => allArtistNames.Contains(a.Name))
            .ToDictionaryAsync(a => a.Name, StringComparer.OrdinalIgnoreCase);

        // Create missing artists and add them to context
        var newArtists = new Dictionary<string, Artist>(StringComparer.OrdinalIgnoreCase);
        var missingNames = allArtistNames.Except(existingArtists.Keys, StringComparer.OrdinalIgnoreCase).ToList();

        if (missingNames.Count != 0)
        {
            logger.LogInformation("Creating missing artists: {MissingArtists}", string.Join(", ", missingNames));

            foreach (var name in missingNames)
            {
                var artist = new Artist
                {
                    Name = name,
                    Sources = [
                        new ArtistSource
                        {
                            Source = "Ticketmaster",
                            SourceArtistId = attractionId, // Use the attraction ID we're querying
                            Url = $"https://www.ticketmaster.com/{Uri.EscapeDataString(name)}-tickets/artist/{attractionId}"
                        }
                    ]
                };

                newArtists[name] = artist;
                context.Artists.Add(artist);
            }
        }

        // Combine existing and new artists for lookups
        var allArtists = existingArtists.Concat(newArtists).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        var concerts = new List<Concert>();

        foreach (var ev in events.EnumerateArray())
        {
            logger.LogInformation("Processing event: {EventId}", ev.GetProperty("id").GetString() ?? "Unknown");

            var eventData = ParseEventData(ev);
            if (eventData is null) continue;

            // Get artists for this event
            var eventArtistNames = ev.GetProperty("_embedded").GetProperty("attractions")
                .EnumerateArray()
                .Select(a => a.GetProperty("name").GetString() ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            var eventArtists = eventArtistNames
                .Where(name => allArtists.ContainsKey(name))
                .Select(name => allArtists[name])
                .ToList();

            var concert = new Concert
            {
                Source = "Ticketmaster",
                SourceEventId = eventData.Id,
                TicketUrl = eventData.TicketUrl,
                Date = eventData.Date,
                VenueName = eventData.VenueName,
                City = eventData.City,
                Country = eventData.Country,
                Artists = eventArtists
            };

            concerts.Add(concert);
        }

        return concerts;
    }

    private EventData? ParseEventData(JsonElement ev)
    {
        try
        {
            var id = ev.GetProperty("id").GetString() ?? string.Empty;
            var ticketUrl = ev.GetProperty("url").GetString() ?? string.Empty;
            var start = ev.GetProperty("dates").GetProperty("start");

            DateTimeOffset date;
            if (start.TryGetProperty("dateTime", out var dateTimeProp))
            {
                date = dateTimeProp.GetDateTimeOffset();
            }
            else if (start.TryGetProperty("localDate", out var localDateProp))
            {
                date = DateTimeOffset.Parse(localDateProp.GetString()!);
            }
            else
            {
                date = DateTimeOffset.MinValue;
            }

            var venues = ev.GetProperty("_embedded").GetProperty("venues");
            var venueName = "-- Unknown Venue --";

            if (venues[0].TryGetProperty("name", out var venueNameProp))
            {
                venueName = venueNameProp.GetString();
            }
            else if (venues[0].TryGetProperty("fullName", out var fullNameProp))
            {
                venueName = fullNameProp.GetString();
            }

            var city = venues[0].GetProperty("city").GetProperty("name").GetString() ?? string.Empty;
            var country = venues[0].GetProperty("country").GetProperty("name").GetString() ?? string.Empty;

            return new EventData(id, ticketUrl, date, venueName!, city, country);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse event data");
            return null;
        }
    }

    private record EventData(string Id, string TicketUrl, DateTimeOffset Date, string VenueName, string City, string Country);
}
