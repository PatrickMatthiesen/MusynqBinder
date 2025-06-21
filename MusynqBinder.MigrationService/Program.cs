using ConcertTracker.Api.Data;
using MusynqBinder.MigrationService;
using MusynqBinder.Web.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<MusicDbContext>("musynqbinder");
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

builder.Services.AddHostedService<Worker<MusicDbContext>>();
builder.Services.AddHostedService<Worker<ApplicationDbContext>>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => {
        tracing.AddSource(Worker<MusicDbContext>.ActivitySourceName);
        tracing.AddSource(Worker<ApplicationDbContext>.ActivitySourceName);
    });

var host = builder.Build();
host.Run();
