using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//builder.Services.ConfigureHttpClientDefaults(static http =>
//{
//    http.
//});

builder.Services.AddHttpClient("MusyncBinder.Web.Server", httpClient =>
{
    // Assuming AddServiceDiscovery is a custom method, apply it here
    httpClient.BaseAddress = new Uri("https://localhost:7067"); // Replace with your actual base address
});

await builder.Build().RunAsync();
