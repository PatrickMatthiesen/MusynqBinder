using ConcertTracker.Api.Data;
using MusynqBinder.Shared.Models;
using System.Text.Json;

namespace ConcertTracker.Api.Services;

public class TicketmasterProvider(MusicDbContext context, HttpClient httpClient, IConfiguration configuration, ILogger<TicketmasterProvider> logger) : IConcertProvider {
    private readonly string _apiKey = configuration["Ticketmaster:ApiKey"] ?? throw new InvalidOperationException("Ticketmaster API key not configured.");


    public async Task<IEnumerable<Concert>> FetchConcertsAsync(string artistName) {
        logger.LogInformation("Fetching concerts for artist: {ArtistName}", artistName);
        var attractionId = await GetAttractionIdAsync(artistName);
        if (string.IsNullOrEmpty(attractionId))
            return [];

        var events = await GetEventsAsync(attractionId);
        return events;
    }

    private async Task<string?> GetAttractionIdAsync(string artistName) {
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
            // Needs to be updated to keep data on all artists at concert
            var name = attraction.GetProperty("name").GetString(); 
            if (string.Equals(name, artistName, StringComparison.OrdinalIgnoreCase))
                return attraction.GetProperty("id").GetString();
        }

        return null;
    }

    private async Task<IEnumerable<Concert>> GetEventsAsync(string attractionId) {
        var url = $"https://app.ticketmaster.com/discovery/v2/events.json?apikey={_apiKey}&attractionId={attractionId}&size=100";
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        if (json == null || !json.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            return [];

        var concerts = new List<Concert>();

        foreach (var ev in events.EnumerateArray())
        {
            logger.LogInformation("Processing event: {EventId}, Raw: {RawText}", ev.GetProperty("id").GetString() ?? "Unknown", ev.GetRawText());
            var id = ev.GetProperty("id").GetString() ?? string.Empty;
            var ticketUrl = ev.GetProperty("url").GetString() ?? string.Empty;
            var start = ev.GetProperty("dates").GetProperty("start");
            DateTimeOffset date;

            if (start.TryGetProperty("dateTime", out var dateTimeProp))
            {
                
                date = dateTimeProp.GetDateTimeOffset();
            }
            else
            {
                date = start.TryGetProperty("localDate", out var localDateProp)
                    ? DateTimeOffset.Parse(localDateProp.GetString()!) // + "T00:00:00Z")
                    : DateTimeOffset.MinValue;
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
            var attractions = ev.GetProperty("_embedded").GetProperty("attractions");
            var artistNames = attractions.EnumerateArray().Select(a => a.GetProperty("name").GetString() ?? string.Empty).ToList();

            var artists = context.Artists
                .Where(a => artistNames.Contains(a.Name))
                .ToList();

            var concert = new Concert
            {
                Source = "Ticketmaster",
                SourceEventId = id,
                TicketUrl = ticketUrl,
                Date = date,
                VenueName = venueName!,
                City = city,
                Country = country,
                Artists = artists
            };

            concerts.Add(concert);
        }

        return concerts;
    }
}
