using ConcertTracker.Api.Data;
using MusynqBinder.Shared.DTO;
using MusynqBinder.Shared.Models;

namespace ConcertTracker.Api.Services;

public class ConcertService(MusicDbContext context, IConcertProvider provider) {
    public async Task<IEnumerable<ConcertDto>> GetConcertsAsync(string artistName) {
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

        var ArtistDtos = concerts
            .SelectMany(c => c.Artists)
            .DistinctBy(a => a.Id)
            .ToDictionary(a => a.Id, a => new ArtistDto {
                Id = a.Id,
                Name = a.Name,
                Concerts = a.Concerts.Select(concert => new ConcertDto {
                    Id = concert.Id,
                    Date = concert.Date,
                    VenueName = concert.VenueName,
                    City = concert.City,
                    Country = concert.Country,
                    Source = concert.Source,
                    SourceEventId = concert.SourceEventId,
                    TicketUrl = concert.TicketUrl
                }).ToList()
            });

        return concerts.Select(c => new ConcertDto {
            Id = c.Id,
            Artists = [.. c.Artists.Where(a => ArtistDtos.ContainsKey(a.Id)).Select(a => ArtistDtos[a.Id])],
            Date = c.Date,
            VenueName = c.VenueName,
            City = c.City,
            Country = c.Country,
            Source = c.Source,
            SourceEventId = c.SourceEventId,
            TicketUrl = c.TicketUrl
        });
    }
}
