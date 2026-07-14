namespace AIForOrcas.Client.Web.Services;

public class AccountService : IAccountService
{
    private readonly HttpClient _httpService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly BlazoradeMsalService _msalService;
    private readonly ILocalStorageService _localStorage;
    private readonly AppSettings _appSettings;

    public AccountService(
        HttpClient httpService,
        AuthenticationStateProvider authenticationStateProvider,
        BlazoradeMsalService msalService,
        ILocalStorageService localStorage,
        AppSettings appSettings)
    {
        _httpService = httpService;
        _authenticationStateProvider = authenticationStateProvider;
        _msalService = msalService;
        _localStorage = localStorage;
        _appSettings = appSettings;
    }

    public async Task<string> GetToken()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");
        return savedToken;
    }

    public async Task<string> GetDisplayname()
    {
        if (_authenticationStateProvider != null)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity.IsAuthenticated)
            {
                var name = user.FindFirst(c => c.Type == "name")?.Value;
                var identity = user.Identity.Name;
                return string.IsNullOrWhiteSpace(name) ? identity : name;
            }
        }

        return string.Empty;
    }

    public async Task<string> GetUsername()
    {
        if (_authenticationStateProvider != null)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                // Try preferred_username first (Azure AD v2.0 tokens).
                var username = user.FindFirst(c => c.Type == "preferred_username")?.Value;

                // Fall back to email claim.
                if (string.IsNullOrWhiteSpace(username))
                    username = user.FindFirst(c => c.Type == "email")?.Value;

                // Fall back to name claim.
                if (string.IsNullOrWhiteSpace(username))
                    username = user.FindFirst(c => c.Type == "name")?.Value;

                // Fall back to identity name.
                if (string.IsNullOrWhiteSpace(username))
                    username = user.Identity.Name;

                return username ?? string.Empty;
            }
        }

        return string.Empty;
    }

    public async Task Login()
    {
        var scopes = new string[] { $"api://{_appSettings.AzureAd.ClientId}/{_appSettings.AzureAd.DefaultScope}" };

        try
        {
            var token = await _msalService.AcquireTokenAsync(prompt: LoginPrompt.Login, scopes: scopes);

            if (token == null)
            {
                Console.WriteLine("Login failed: Token acquisition returned null.");
                return;
            }

            await _localStorage.SetItemAsync("authToken", token.AccessToken);

            await ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Login error: {exception.Message}");
        }
    }

    public async Task Logout()
    {
        await _localStorage.ClearAsync();
        ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
        _httpService.DefaultRequestHeaders.Clear();
    }
}
