using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using MusynqBinder.Data.Identity;
using static Google.Apis.Requests.BatchRequest;

namespace MusynqBinder.Web.Services;

public class YouTubeApiService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;

    public async IAsyncEnumerable<Playlist> GetUserPlaylistsAsync() {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null)
            yield break;

        // Get Google external login info
        var userLogins = await _userManager.GetLoginsAsync(user);
        var googleLogin = userLogins.FirstOrDefault(l => l.LoginProvider == "Google");
        if (googleLogin == null)
            yield break;

        // Try to get the Google tokens from the current authentication context
        var tokens = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
        var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync("refresh_token");

        // Debug: Log what tokens we found
        Console.WriteLine($"Access token found: {tokens != null}");
        Console.WriteLine($"Refresh token found: {refreshToken != null}");

        if (tokens == null)
            yield break;

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
                ClientId = _configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured."),
                ClientSecret = _configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured.")
            },
            Scopes = [YouTubeService.Scope.YoutubeReadonly]
        });

        var credential = new UserCredential(flow, user.Id, tokenResponse);

        // Create YouTube service
        var youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MusynqBinder",
        });

        // Fetch the user's playlists
        var request = youtubeService.Playlists.List("snippet,contentDetails");
        request.Mine = true;
        var response = await request.ExecuteAsync();

        if (response.Items != null)
        {
            foreach (var playlist in response.Items)
            {
                yield return playlist;
            }
        }

        while (response.NextPageToken != null)
        {
            request.PageToken = response.NextPageToken;
            response = await request.ExecuteAsync();
            if (response.Items != null)
            {
                foreach (var playlist in response.Items)
                {
                    yield return playlist;
                }
            }
        }
    }

    public async IAsyncEnumerable<PlaylistItem> GetUserPlaylistItemsAsync(string playlistId) {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null)
            yield break;

        // Get Google external login info
        var userLogins = await _userManager.GetLoginsAsync(user);
        var googleLogin = userLogins.FirstOrDefault(l => l.LoginProvider == "Google");
        if (googleLogin == null)
            yield break;

        // Try to get the Google tokens from the current authentication context
        var tokens = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
        var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync("refresh_token");

        // Debug: Log what tokens we found
        Console.WriteLine($"Access token found: {tokens != null}");
        Console.WriteLine($"Refresh token found: {refreshToken != null}");

        if (tokens == null)
            yield break;

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
                ClientId = _configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured."),
                ClientSecret = _configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured.")
            },
            Scopes = [YouTubeService.Scope.YoutubeReadonly]
        });

        var credential = new UserCredential(flow, user.Id, tokenResponse);

        // Create YouTube service
        var youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MusynqBinder",
        });

        // Fetch the user's playlists
        var request = youtubeService.PlaylistItems.List("snippet,contentDetails"); // TODO: reconsidder if we need contentDetails
        request.PlaylistId = playlistId;
        // request.Id; // In case we need it, then Id can take multiple playlists and return items from all of them
        // request.videoId // if we want to filter for specific videos.
        var response = await request.ExecuteAsync();

        if (response.Items != null)
        {
            foreach (var playlistItem in response.Items)
            {
                yield return playlistItem;
            }
        }

        while (response.NextPageToken != null)
        {
            request.PageToken = response.NextPageToken;
            response = await request.ExecuteAsync();
            if (response.Items != null)
            {
                foreach (var playlistItem in response.Items)
                {
                    yield return playlistItem;
                }
            }
        }
    }
}