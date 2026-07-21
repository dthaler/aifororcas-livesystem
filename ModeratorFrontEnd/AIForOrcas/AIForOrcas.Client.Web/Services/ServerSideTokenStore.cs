using System.Collections.Concurrent;

namespace AIForOrcas.Client.Web.Services;

public interface ITokenStore
{
    void SetToken(string circuitId, string token);
    string GetToken(string circuitId);
    void RemoveToken(string circuitId);
}

public class ServerSideTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    public void SetToken(string circuitId, string token)
    {
        _tokens[circuitId] = token;
    }

    public string GetToken(string circuitId)
    {
        _tokens.TryGetValue(circuitId, out var token);
        return token;
    }

    public void RemoveToken(string circuitId)
    {
        _tokens.TryRemove(circuitId, out _);
    }
}