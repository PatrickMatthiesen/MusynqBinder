using MusynqBinder.Web.Wasm.Pages;
using MusynqBinder.Web.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient("MusynqBinder.Web.Server", client =>
{
    // Assuming AddServiceDiscovery is a custom method, apply it here
    client.BaseAddress = new Uri("https://concerttracker-api"); // Replace with your actual base address
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MusynqBinder.Web.Wasm._Imports).Assembly);

//app.MapGet("/api/concerts/{artistName}", (string artistName, HttpContext httpContext) =>
//{
//    Console.WriteLine($"Received request for concerts by artist: {artistName}");
//    return "test 1 2 3 " + artistName;
//});

app.Run();
