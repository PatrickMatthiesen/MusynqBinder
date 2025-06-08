using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//builder.Services.AddHttpClient("ConcertTracker", httpClient =>
//{
//    // Assuming AddServiceDiscovery is a custom method, apply it here
//    httpClient.BaseAddress = new Uri("https://localhost:7067"); // Replace with your actual base address
//});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7067") });

await builder.Build().RunAsync();
