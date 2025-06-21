using MusynqBinder.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcertTracker.Api.Data;

public class MusicDbContext(DbContextOptions<MusicDbContext> options) : DbContext(options) {
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Concert> Concerts => Set<Concert>();
}
