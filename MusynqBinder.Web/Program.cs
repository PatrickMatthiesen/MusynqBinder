using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MusynqBinder.Web.Components;
using MusynqBinder.Web.Components.Account;
using MusynqBinder.Data.Identity;
using MusynqBinder.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<YouTubeApiService>();

// builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://concerttracker-api") });
builder.Services.AddHttpClient("ConcertTrackerApi", client =>
{
    client.BaseAddress = new Uri("https://concerttracker-api");
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured.");
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured.");
        options.SaveTokens = true;
        options.Scope.Add("https://www.googleapis.com/auth/youtube.readonly");

        options.AccessType = "offline";

        // Force consent screen to ensure refresh token is provided
        var oldOnRedirectToAuthorizationEndpoint = options.Events.OnRedirectToAuthorizationEndpoint;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.RedirectUri += "&prompt=consent";
            return oldOnRedirectToAuthorizationEndpoint(context);
        };

        options.Events.OnCreatingTicket = ctx =>
        {
            List<AuthenticationToken> tokens = ctx.Properties.GetTokens().ToList();

            return Task.CompletedTask;
        };
    })
    .AddIdentityCookies();

builder.AddNpgsqlDbContext<ApplicationDbContext>(connectionName: "identitydb");

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
