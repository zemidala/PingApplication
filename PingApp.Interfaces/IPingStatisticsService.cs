using System.Collections.Generic;
using PingApp.Models;

namespace PingApp.Interfaces
{
    public interface IPingStatisticsService
    {
        void AddPingResult(string host, PingResult result);
        PingStatistics GetStatistics(string host);
        void ClearStatistics(string host);
        List<PingResult> GetPingHistory(string host);
    }
}