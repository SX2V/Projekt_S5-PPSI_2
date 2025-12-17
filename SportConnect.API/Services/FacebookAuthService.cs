//now

public interface IFacebookAuthService
{
	Task<FacebookUser?> ValidateAccessTokenAsync(string accessToken);
}

public class FacebookAuthService : IFacebookAuthService
{
	private readonly HttpClient _httpClient;

	public FacebookAuthService(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<FacebookUser?> ValidateAccessTokenAsync(string accessToken)
	{
		var response = await _httpClient.GetAsync(
			$"https://graph.facebook.com/me?fields=id,email,name&access_token={accessToken}");

		if (!response.IsSuccessStatusCode)
			return null;

		var json = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<FacebookUser>(json);
	}
}
