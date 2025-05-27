namespace MusynqBinder.Shared.Models;

public class Artist {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Concert> Concerts { get; set; } = new List<Concert>();
}
