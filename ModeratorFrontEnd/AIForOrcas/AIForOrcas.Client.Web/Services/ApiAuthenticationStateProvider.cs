using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace AIForOrcas.Client.Web.Services;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStore _tokenStore;
    private readonly CircuitHandlerService _circuitHandler;
    private readonly ILogger<ApiAuthenticationStateProvider> _logger;
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

    public ApiAuthenticationStateProvider(
        ITokenStore tokenStore,
        CircuitHandlerService circuitHandler,
        ILogger<ApiAuthenticationStateProvider> logger)
    {
        _tokenStore = tokenStore;
        _circuitHandler = circuitHandler;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public Task MarkUserAsAuthenticated(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted to mark user as authenticated with empty token");
            return Task.CompletedTask;
        }

        try
        {
            // Store token in server-side store
            var circuitId = _circuitHandler.CircuitId;
            if (!string.IsNullOrWhiteSpace(circuitId))
            {
                _tokenStore.SetToken(circuitId, token);
            }

            // Parse claims from JWT
            var claims = ParseClaimsFromJwt(token).ToList();
            
            // Add the token as a claim so the handler can access it
            claims.Add(new Claim("authToken", token));
            
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            
            var authState = Task.FromResult(new AuthenticationState(_currentUser));
            NotifyAuthenticationStateChanged(authState);
            
            _logger.LogInformation("User authenticated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user as authenticated");
        }

        return Task.CompletedTask;
    }

    public void MarkUserAsLoggedOut()
    {
        var circuitId = _circuitHandler.CircuitId;
        if (!string.IsNullOrWhiteSpace(circuitId))
        {
            _tokenStore.RemoveToken(circuitId);
        }

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        
        var authState = Task.FromResult(new AuthenticationState(_currentUser));
        NotifyAuthenticationStateChanged(authState);
        
        _logger.LogInformation("User logged out");
    }

    public string GetToken()
    {
        var circuitId = _circuitHandler.CircuitId;
        if (string.IsNullOrWhiteSpace(circuitId))
        {
            return null;
        }

        return _tokenStore.GetToken(circuitId);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        
        try
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs == null) return claims;

            // Handle groups
            if (keyValuePairs.TryGetValue("groups", out object groups) && groups != null)
            {
                if (groups.ToString().Trim().StartsWith("["))
                {
                    var parsedGroups = JsonSerializer.Deserialize<string[]>(groups.ToString());
                    if (parsedGroups != null)
                    {
                        foreach (var parsedGroup in parsedGroups)
                        {
                            claims.Add(new Claim("groups", parsedGroup));
                        }
                    }
                }
                else
                {
                    claims.Add(new Claim("groups", groups.ToString()));
                }
                keyValuePairs.Remove("groups");
            }

            // Handle roles
            if (keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles) && roles != null)
            {
                if (roles.ToString().Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());
                    if (parsedRoles != null)
                    {
                        foreach (var parsedRole in parsedRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                        }
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                }
                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing claims from JWT");
        }

        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
