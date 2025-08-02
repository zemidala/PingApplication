using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using PingApp.Interfaces;
using PingApp.Models;

namespace PingApp.Services;

public class PingService : IPingService, IDisposable
{
    private readonly object _lockObject = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pingTokens;

    public PingService()
    {
        _pingTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    public void Dispose()
    {
        foreach (var token in _pingTokens.Values)
        {
            token.Cancel();
            token.Dispose();
        }

        _pingTokens.Clear();
    }

    public event EventHandler<PingEventArgs> PingCompleted;

    public async Task<PingResult> PingHostAsync(string host, int packetSize = 32, int timeout = 5000)
    {
        var result = new PingResult
        {
            Host = host,
            Timestamp = DateTime.Now,
            PacketSize = packetSize
        };

        try
        {
            using (var ping = new Ping())
            {
                var buffer = new byte[packetSize];
                var reply = await ping.SendPingAsync(host, timeout, buffer);

                if (reply.Status == IPStatus.Success)
                {
                    result.IsSuccess = true;
                    result.RoundTripTime = reply.RoundtripTime;
                    result.IpAddress = reply.Address?.ToString();
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = reply.Status.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        OnPingCompleted(new PingEventArgs { Host = host, Result = result });
        return result;
    }

    public void StartContinuousPing(string host, int packetSize, int interval, int timeout)
    {
        if (_pingTokens.ContainsKey(host))
            return;

        var cancellationTokenSource = new CancellationTokenSource();
        _pingTokens[host] = cancellationTokenSource;

        Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
                try
                {
                    await PingHostAsync(host, packetSize, timeout);
                    await Task.Delay(interval, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Log exception
                }
        }, cancellationTokenSource.Token);
    }

    public void StopContinuousPing(string host)
    {
        if (_pingTokens.TryRemove(host, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }

    protected virtual void OnPingCompleted(PingEventArgs e)
    {
        PingCompleted?.Invoke(this, e);
    }
}