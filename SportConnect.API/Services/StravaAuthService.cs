//new

public interface IStravaAuthService
{
    Task<StravaTokenResponse?> ExchangeCodeForTokenAsync(string code);
    Task<StravaUser?> GetAthleteProfileAsync(string accessToken);
}

public class StravaAuthService : IStravaAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public StravaAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<StravaTokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", _configuration["Strava:ClientId"] },
            { "client_secret", _configuration["Strava:ClientSecret"] },
            { "code", code },
            { "grant_type", "authorization_code" }
        };

        var response = await _httpClient.PostAsync(
            "https://www.strava.com/oauth/token",
            new FormUrlEncodedContent(parameters)
        );

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StravaTokenResponse>(json);
    }

    public async Task<StravaUser?> GetAthleteProfileAsync(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync("https://www.strava.com/api/v3/athlete");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StravaUser>(json);
    }
}

public class StravaTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("athlete")]
    public StravaUser Athlete { get; set; }
}
