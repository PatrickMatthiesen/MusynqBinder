using ConcertTracker.Api.Data;
using MusynqBinder.Shared.Models;

namespace ConcertTracker.Api.Services;

public class ConcertService(AppDbContext context, IConcertProvider provider) {
    public async Task<IEnumerable<Concert>> GetConcertsAsync(string artistName) {
        var concerts = await provider.FetchConcertsAsync(artistName);
        // Save to database if needed
        return concerts;
    }
}
