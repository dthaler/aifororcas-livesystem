using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AIForOrcas.Client.Web.Services;

public class CircuitHandlerService : CircuitHandler
{
    private static int _instanceCount = 0;
    private readonly int _instanceId;

    public CircuitHandlerService()
    {
        _instanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[CircuitHandlerService #{_instanceId}] Constructed");
    }

    public string CircuitId { get; private set; }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitId = circuit.Id;
        Console.WriteLine($"[CircuitHandlerService #{_instanceId}] CIRCUIT OPENED: {CircuitId}");
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CircuitHandlerService #{_instanceId}] CIRCUIT CLOSED: {CircuitId}");
        CircuitId = null;
        return Task.CompletedTask;
    }
}
