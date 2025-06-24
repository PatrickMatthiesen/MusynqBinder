using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusynqBinder.Shared.DTO;

public record ArtistDto {
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<ConcertDto> Concerts { get; init; } = [];
    public List<ArtistSourceDto> Sources { get; init; } = [];
}

public record ArtistSourceDto {
    public int Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string SourceArtistId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}