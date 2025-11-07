using MusynqBinder.Data.Music;
using MusynqBinder.Shared.DTO;
using MusynqBinder.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcertTracker.Api.Services;

public class ConcertService(MusicDbContext context, IConcertProvider provider) {
    public async Task<IEnumerable<ConcertDto>> GetConcertsAsync(string artistName) {
        var concerts = await provider.FetchConcertsAsync(artistName);
        // Save to database if needed

        // Batch check for existing concerts instead of querying in a loop
        var concertList = concerts.ToList();
        var concertIds = concertList.Select(c => c.Id).ToHashSet();
        var existingConcertIds = await context.Concerts
            .Where(c => concertIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSetAsync();

        foreach (var concert in concertList) {
            if (!existingConcertIds.Contains(concert.Id)) {
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
