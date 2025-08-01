using System;
using System.Threading.Tasks;
using PingApp.Models;

namespace PingApp.Interfaces
{
    public interface IPingService
    {
        Task<PingResult> PingHostAsync(string host, int packetSize = 32, int timeout = 5000);
        void StartContinuousPing(string host, int packetSize, int interval, int timeout);
        void StopContinuousPing(string host);
        event EventHandler<PingEventArgs> PingCompleted;
    }
}