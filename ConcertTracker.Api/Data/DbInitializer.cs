﻿namespace ConcertTracker.Api.Data;

public static class DbInitializer {
    public static void Initialize(AppDbContext context) {
        context.Database.EnsureCreated();
        // Seed initial data if necessary
    }
}
