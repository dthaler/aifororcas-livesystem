using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

namespace AIForOrcas.Client.Web.Services;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly ILogger<AuthenticationHeaderHandler> _logger;
    private static int _instanceCount = 0;
    private readonly int _instanceId;
    private readonly ITokenStore _tokenStore;
    private readonly CircuitHandlerService _circuit;

    public AuthenticationHeaderHandler(
        ITokenStore tokenStore,
        CircuitHandlerService circuit,
        ILogger<AuthenticationHeaderHandler> logger)
    {
        _tokenStore = tokenStore;
        _circuit = circuit;
        _logger = logger;
        _instanceId = Interlocked.Increment(ref _instanceCount);
        _logger.LogDebug("[AuthHandler #{InstanceId}] Constructed", _instanceId);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var circuitId = _circuit.CircuitId;
        if (string.IsNullOrWhiteSpace(circuitId))
            _logger.LogWarning("Handler #{InstanceId} has no CircuitId; sending request without Authorization header", _instanceId);

        var token = !string.IsNullOrWhiteSpace(circuitId) ? _tokenStore.GetToken(circuitId) : null;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        
        _logger.LogDebug("[AuthHandler #{InstanceId}] Response: {StatusCode}", 
            _instanceId, response.StatusCode);
        
        return response;
    }
}