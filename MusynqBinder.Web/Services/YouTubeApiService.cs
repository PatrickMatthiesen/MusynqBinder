using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using MusynqBinder.Web.Data;

namespace MusynqBinder.Web.Services;

public class YouTubeApiService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) {
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;

    public async Task<List<Playlist>> GetUserPlaylistsAsync()
    {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null) return [];

        // Get the Google tokens from ASP.NET Core Identity
        var tokens = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
        var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync("refresh_token");
        
        if (tokens == null) return [];

        // Prepare Google's credential objects
        var tokenResponse = new TokenResponse
        {
            AccessToken = tokens,
            RefreshToken = refreshToken
        };

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _configuration["Authentication:Google:ClientId"],
                ClientSecret = _configuration["Authentication:Google:ClientSecret"]
            },
            Scopes = [YouTubeService.Scope.YoutubeReadonly]
        });

        var credential = new UserCredential(flow, user.Id, tokenResponse);

        // Create YouTube service
        var youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "BlazorYouTubeSample"
        });

        // Fetch the user's playlists
        var request = youtubeService.Playlists.List("snippet,contentDetails");
        request.Mine = true;
        var response = await request.ExecuteAsync();

        return response.Items?.ToList() ?? [];
    }
}