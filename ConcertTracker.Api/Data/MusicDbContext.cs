using MusynqBinder.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcertTracker.Api.Data;

public class MusicDbContext(DbContextOptions<MusicDbContext> options) : DbContext(options) {
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Concert> Concerts => Set<Concert>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Concert>()
            .Property(e => e.Date)
            .HasConversion(
                v => v.ToUniversalTime(), // Convert to UTC when saving
                v => v // Keep as-is when reading (already UTC from DB)
            );
    }
}
