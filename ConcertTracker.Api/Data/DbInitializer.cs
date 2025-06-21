namespace ConcertTracker.Api.Data;

public static class DbInitializer {
    public static void Initialize(MusicDbContext context) {
        context.Database.EnsureCreated();
        // Seed initial data if necessary
    }
}
