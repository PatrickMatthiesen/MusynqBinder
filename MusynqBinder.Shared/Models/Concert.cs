namespace MusynqBinder.Shared.Models;

public class Concert {
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;
    public DateTime Date { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceEventId { get; set; } = string.Empty;
    public string TicketUrl { get; set; } = string.Empty;
}
