using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using OpenTelemetry.Trace;

using MusynqBinder.Web.Data;
using MusynqBinder.Shared.Models;
using ConcertTracker.Api.Data;

namespace MusynqBinder.MigrationService;

public class Worker<TContext>(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService where TContext : DbContext
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            await RunMigrationAsync(dbContext, cancellationToken);

            if (dbContext is MusicDbContext musicDbContext)
                await SeedDataAsync(musicDbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(TContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static async Task SeedDataAsync(MusicDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check if data already exists
        if (await dbContext.Artists.AnyAsync(a => a.Name == "Dabin", cancellationToken))
        {
            Console.WriteLine("Seed data already exists, skipping...");
            return; // Data already seeded
        }

        Artist firstArtist = new()
        {
            Name = "Dabin",
            Sources = [
                new ArtistSource {
                    Source = "Ticketmaster",
                    SourceArtistId = "K8vZ917oaYf",
                    Url = "https://www.ticketmaster.com/dabin-tickets/artist/1861251"
                }
            ]
        };

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Check if operation was cancelled before proceeding
            cancellationToken.ThrowIfCancellationRequested();

            // Seed the database
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.Artists.AddAsync(firstArtist, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}