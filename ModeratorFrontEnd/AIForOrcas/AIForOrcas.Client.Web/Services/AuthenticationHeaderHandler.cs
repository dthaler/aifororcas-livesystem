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
        _logger.LogError("!!! [AuthHandler #{InstanceId}] CONSTRUCTED !!!", _instanceId);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Handler #{InstanceId} using CircuitId: {CircuitId}", _instanceId, _circuit.CircuitId);

        var token = _tokenStore.GetToken(_circuit.CircuitId);

        _logger.LogError("Handler #{InstanceId} token: {Token}", _instanceId, token);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        
        _logger.LogError("!!! [AuthHandler #{InstanceId}] Response: {StatusCode} !!!", 
            _instanceId, response.StatusCode);
        
        return response;
    }
}