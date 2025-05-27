using ConcertTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcertTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Concert> Concerts => Set<Concert>();
}
