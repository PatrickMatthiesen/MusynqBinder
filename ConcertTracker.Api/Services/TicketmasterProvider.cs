using ConcertTracker.Api.Models;
using System.Text.Json;

namespace ConcertTracker.Api.Services;

public class TicketmasterProvider(HttpClient httpClient, IConfiguration configuration) : IConcertProvider {
    private readonly string _apiKey = configuration["Ticketmaster:ApiKey"] ?? throw new InvalidOperationException("Ticketmaster API key not configured.");

    public async Task<IEnumerable<Concert>> FetchConcertsAsync(string artistName) {
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
            var concert = new Concert
            {
                Source = "Ticketmaster",
                SourceEventId = ev.GetProperty("id").GetString() ?? string.Empty,
                TicketUrl = ev.GetProperty("url").GetString() ?? string.Empty,
                Date = DateTime.Parse(ev.GetProperty("dates").GetProperty("start").GetProperty("dateTime").GetString() ?? DateTime.MinValue.ToString()),
                VenueName = ev.GetProperty("_embedded").GetProperty("venues")[0].GetProperty("name").GetString() ?? string.Empty,
                City = ev.GetProperty("_embedded").GetProperty("venues")[0].GetProperty("city").GetProperty("name").GetString() ?? string.Empty,
                Country = ev.GetProperty("_embedded").GetProperty("venues")[0].GetProperty("country").GetProperty("name").GetString() ?? string.Empty,
                Artist = new Artist
                {
                    Name = ev.GetProperty("_embedded").GetProperty("attractions")[0].GetProperty("name").GetString() ?? string.Empty
                }
            };

            concerts.Add(concert);
        }

        return concerts;
    }
}
