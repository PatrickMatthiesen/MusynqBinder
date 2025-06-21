using ConcertTracker.Api.Data;
using MusynqBinder.Shared.Models;

namespace ConcertTracker.Api.Services;

public class ConcertService(MusicDbContext context, IConcertProvider provider) {
    public async Task<IEnumerable<Concert>> GetConcertsAsync(string artistName) {
        var concerts = await provider.FetchConcertsAsync(artistName);
        // Save to database if needed

        foreach (var concert in concerts) {

            var existingConcert = context.Concerts
                .FirstOrDefault(c => c.Id == concert.Id);
            
            if (existingConcert == null) {
                context.Concerts.Add(concert);
            }
        }
        
        await context.SaveChangesAsync();

        return concerts;
    }
}
