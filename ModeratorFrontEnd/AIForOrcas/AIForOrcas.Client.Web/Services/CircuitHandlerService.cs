using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AIForOrcas.Client.Web.Services;

public class CircuitHandlerService : CircuitHandler
{
    private readonly ITokenStore _tokenStore;
    private readonly Microsoft.Extensions.Logging.ILogger<CircuitHandlerService> _logger;

    public CircuitHandlerService(ITokenStore tokenStore, Microsoft.Extensions.Logging.ILogger<CircuitHandlerService> logger)
    {
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public string CircuitId { get; private set; }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitId = circuit.Id;
        Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(_logger, "Circuit opened: {CircuitId}", CircuitId);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(_logger, "Circuit closed: {CircuitId}", circuit.Id);
        _tokenStore.RemoveToken(circuit.Id);
        CircuitId = null;
        return Task.CompletedTask;
    }
}
