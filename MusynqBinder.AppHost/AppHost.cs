var builder = DistributedApplication.CreateBuilder(args);

var ticketmasterApiKey = builder.AddParameter("Ticketmaster-ApiKey", secret: true);

var database = builder.AddPostgres("database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
    
var musicDatabase = database.AddDatabase("musynqbinder");
var identityDatabase = database.AddDatabase("identitydb");

var cache = builder.AddRedis("cache")
    .WithDataVolume()
    .WithRedisCommander();

var concertApi = builder.AddProject<Projects.ConcertTracker_Api>("concerttracker-api")
    .WithEnvironment("Ticketmaster:ApiKey", ticketmasterApiKey)
    .WithHttpHealthCheck("/health")
    .WithReference(musicDatabase)
    .WaitFor(musicDatabase)
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.MusynqBinder_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(identityDatabase)
    .WaitFor(identityDatabase)
    .WithReference(concertApi)
    .WaitFor(concertApi);


builder.Build().Run();
