﻿namespace MusynqBinder.Shared.Models;

public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Concert> Concerts { get; set; } = [];
    public List<ArtistSource> Sources { get; set; } = [];
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.MinValue;
}

public class ArtistSource // 
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceArtistId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}