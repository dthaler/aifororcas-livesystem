using Microsoft.Extensions.Logging;

namespace AIForOrcas.Client.Web.Services;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authProvider;
    private readonly ILogger<AuthenticationHeaderHandler> _logger;
    private static int _instanceCount = 0;
    private readonly int _instanceId;

    public AuthenticationHeaderHandler(
        AuthenticationStateProvider authProvider,
        ILogger<AuthenticationHeaderHandler> logger)
    {
        _authProvider = authProvider;
        _logger = logger;
        _instanceId = Interlocked.Increment(ref _instanceCount);
        _logger.LogError("!!! [AuthHandler #{InstanceId}] CONSTRUCTED !!!", _instanceId);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogError("!!! [AuthHandler #{InstanceId}] SendAsync CALLED for {Method} {Uri} !!!", 
            _instanceId, request.Method, request.RequestUri);

        var state = await _authProvider.GetAuthenticationStateAsync();
        var user = state.User;

        _logger.LogError("!!! [AuthHandler #{InstanceId}] User.Identity.IsAuthenticated = {IsAuth} !!!", 
            _instanceId, user?.Identity?.IsAuthenticated ?? false);

        if (user?.Identity?.IsAuthenticated == true)
        {
            var tokenClaim = user.FindFirst("authToken");
            
            if (tokenClaim != null && !string.IsNullOrWhiteSpace(tokenClaim.Value))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                _logger.LogError("!!! [AuthHandler #{InstanceId}] ✓ Authorization header ADDED !!!", _instanceId);
            }
            else
            {
                _logger.LogError("!!! [AuthHandler #{InstanceId}] ✗ No token claim found !!!", _instanceId);
            }
        }
        else
        {
            _logger.LogError("!!! [AuthHandler #{InstanceId}] ✗ User not authenticated !!!", _instanceId);
        }

        var response = await base.SendAsync(request, cancellationToken);
        
        _logger.LogError("!!! [AuthHandler #{InstanceId}] Response: {StatusCode} !!!", 
            _instanceId, response.StatusCode);
        
        return response;
    }
}