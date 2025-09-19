using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env")
    .WithDashboard(c => c.WithHostPort(8083));

var ticketmasterApiKey = builder.AddParameter("Ticketmaster-ApiKey", secret: true);
var googleClientId = builder.AddParameter("Google-OAuth-ClientId", secret: true);
var googleClientSecret = builder.AddParameter("Google-OAuth-ClientSecret", secret: true);

var database = builder.AddPostgres("database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHostPort(6543);

var musicDatabase = database.AddDatabase("musynqbinder");
var identityDatabase = database.AddDatabase("identitydb");

var cache = builder.AddRedis("cache");
if (!builder.Environment.IsDevelopment())
    cache.WithDataVolume();
cache.WithRedisCommander();

var migrations = builder.AddProject<Projects.MusynqBinder_MigrationService>("musynqbinder-migrationservice")
    .WithReferenceAndWait(musicDatabase)
    .WithReferenceAndWait(identityDatabase);

var concertApi = builder.AddProject<Projects.ConcertTracker_Api>("concerttracker-api")
    .WithEnvironment("Ticketmaster:ApiKey", ticketmasterApiKey)
    .WithHttpHealthCheck("/health")
    .WithReferenceAndWait(musicDatabase)
    .WithReferenceAndWait(cache)
    .WaitForCompletion(migrations);

var webDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "MusynqBinder.Web"));
var command = builder.Environment.IsDevelopment()
    ? "bun build:css:dev"
    : "bun build:css";
var tailwindCss = builder.AddExecutable("tailwindcss", "bun", webDirectory, "build:css:dev");

builder.AddProject<Projects.MusynqBinder_Web>("webfrontend")
    .WithEnvironment("Google:ClientId", googleClientId)
    .WithEnvironment("Google:ClientSecret", googleClientSecret)
    .WithExternalHttpEndpoints()
    //.WithHttpHealthCheck("/health")
    .WithReferenceAndWait(cache)
    .WithReferenceAndWait(identityDatabase)
    .WithReferenceAndWait(concertApi)
    .WaitForCompletion(migrations);


builder.Build().Run();


public static class AppHostExtensions {
    public static IResourceBuilder<ProjectResource> WithReferenceAndWait(this IResourceBuilder<ProjectResource> project, 
            IResourceBuilder<IResourceWithConnectionString> resource) =>
        project
            .WithReference(resource)
            .WaitFor(resource);

    public static IResourceBuilder<ProjectResource> WithReferenceAndWait(this IResourceBuilder<ProjectResource> project,
            IResourceBuilder<ProjectResource> resource) =>
        project
            .WithReference(resource)
            .WaitFor(resource);
}