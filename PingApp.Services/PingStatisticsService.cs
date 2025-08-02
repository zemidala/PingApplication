using System.Collections.Concurrent;
using PingApp.Interfaces;
using PingApp.Models;

namespace PingApp.Services;

public class PingStatisticsService : IPingStatisticsService
{
    private readonly ConcurrentDictionary<string, List<PingResult>> _pingHistory;
    private readonly ConcurrentDictionary<string, PingStatistics> _statistics;

    public PingStatisticsService()
    {
        _statistics = new ConcurrentDictionary<string, PingStatistics>();
        _pingHistory = new ConcurrentDictionary<string, List<PingResult>>();
    }

    public void AddPingResult(string host, PingResult result)
    {
        var stats = _statistics.GetOrAdd(host, _ => new PingStatistics());
        stats.UpdateWithResult(result);

        var history = _pingHistory.GetOrAdd(host, _ => new List<PingResult>());
        lock (history)
        {
            history.Add(result);
            // Ограничиваем историю последними 1000 пингами
            if (history.Count > 1000) history.RemoveRange(0, history.Count - 1000);
        }
    }

    public PingStatistics GetStatistics(string host)
    {
        return _statistics.GetOrAdd(host, _ => new PingStatistics());
    }

    public void ClearStatistics(string host)
    {
        _statistics.AddOrUpdate(host, new PingStatistics(), (key, old) => new PingStatistics());
        if (_pingHistory.ContainsKey(host))
            lock (_pingHistory[host])
            {
                _pingHistory[host].Clear();
            }
    }

    public List<PingResult> GetPingHistory(string host)
    {
        if (_pingHistory.TryGetValue(host, out var history))
            lock (history)
            {
                return new List<PingResult>(history);
            }

        return new List<PingResult>();
    }
}