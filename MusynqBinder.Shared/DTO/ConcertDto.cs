using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusynqBinder.Shared.DTO;
public record ConcertDto
{
    public int Id { get; init; }
    public List<ArtistDto> Artists { get; init; } = [];
    public DateTimeOffset Date { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string SourceEventId { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
}
