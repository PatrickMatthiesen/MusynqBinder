namespace MusynqBinder.Shared.Models;

public class Concert {
    public int Id { get; set; }
    public List<Artist> Artists { get; set; } = [];
    public DateTimeOffset Date { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceEventId { get; set; } = string.Empty;
    public string TicketUrl { get; set; } = string.Empty;
}
