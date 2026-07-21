using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace AIForOrcas.Client.Web.Services;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authProvider;
    private readonly ILogger<AuthenticationHeaderHandler> _logger;

    public AuthenticationHeaderHandler(
        AuthenticationStateProvider authProvider,
        ILogger<AuthenticationHeaderHandler> logger)
    {
        _authProvider = authProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AuthHandler] Processing {Method} request to {Uri}", 
            request.Method, request.RequestUri);

        try
        {
            var state = await _authProvider.GetAuthenticationStateAsync();
            var user = state.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                // Extract token from claims
                var tokenClaim = user.FindFirst("authToken");

                if (tokenClaim != null && !string.IsNullOrWhiteSpace(tokenClaim.Value))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                    _logger.LogInformation("[AuthHandler] Authorization header added");
                }
                else
                {
                    _logger.LogWarning("[AuthHandler] User authenticated but no token claim found");
                }
            }
            else
            {
                _logger.LogInformation("[AuthHandler] User not authenticated - no auth header added");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthHandler] Error retrieving authentication state");
        }

        var response = await base.SendAsync(request, cancellationToken);

        _logger.LogInformation("[AuthHandler] Response: {StatusCode}", response.StatusCode);

        return response;
    }
}