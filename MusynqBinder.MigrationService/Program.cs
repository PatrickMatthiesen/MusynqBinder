using ConcertTracker.Api.Data;
using MusynqBinder.MigrationService;
using MusynqBinder.Web.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker<MusicDbContext>>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker<MusicDbContext>.ActivitySourceName));
builder.AddNpgsqlDbContext<MusicDbContext>("musynqbinder");
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

var host = builder.Build();
host.Run();
