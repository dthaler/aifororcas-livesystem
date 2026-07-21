using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AIForOrcas.Client.Web.Services;

public class CircuitHandlerService : CircuitHandler
{
    public string CircuitId { get; private set; }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitId = circuit.Id;
        return base.OnConnectionUpAsync(circuit, cancellationToken);
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitId = null;
        return base.OnConnectionDownAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitId = null;
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}