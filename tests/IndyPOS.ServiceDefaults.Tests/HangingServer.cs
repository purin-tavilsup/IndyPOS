using System.Net;
using System.Net.Sockets;

namespace IndyPOS.ServiceDefaults.Tests;

/// <summary>
/// Accepts TCP connections and never answers, like a database host that has hung. Npgsql's connect
/// phase ignores cancellation, so only the connection string's own Timeout can end a check against it.
/// </summary>
internal sealed class HangingServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly List<Socket> _held = [];

    public HangingServer()
    {
        _listener.Start();
        _ = AcceptForeverAsync();
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public string ConnectionString(string extra = "") =>
        $"Host=127.0.0.1;Port={Port};Username=probe;Password=probe;Database=probe;{extra}";

    public static string ClosedPortConnectionString()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"Host=127.0.0.1;Port={port};Username=probe;Password=probe;Database=probe";
    }

    private async Task AcceptForeverAsync()
    {
        try
        {
            while (true)
                _held.Add(await _listener.AcceptSocketAsync());
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
    }

    public void Dispose()
    {
        _listener.Stop();
        foreach (var socket in _held)
            socket.Dispose();
    }
}
