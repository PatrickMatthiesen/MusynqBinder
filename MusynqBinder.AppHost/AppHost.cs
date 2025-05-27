var builder = DistributedApplication.CreateBuilder(args);

var ticketmasterApiKey = builder.AddParameter("Ticketmaster-ApiKey", secret: true);

var database = builder.AddPostgres("database")
    .WithDataVolume()
    .AddDatabase("musynqbinder");

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var concertApi = builder.AddProject<Projects.ConcertTracker_Api>("concerttracker-api")
    .WithEnvironment("Ticketmaster:ApiKey", ticketmasterApiKey)
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(cache);

builder.AddProject<Projects.MusynqBinder_Web_Server>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(database)
    .WaitFor(database)
    .WithReference(concertApi)
    .WaitFor(concertApi);


builder.Build().Run();
