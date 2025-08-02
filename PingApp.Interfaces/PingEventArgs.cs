using PingApp.Models;

namespace PingApp.Interfaces;

public class PingEventArgs : EventArgs
{
    public string Host { get; set; }
    public PingResult Result { get; set; }
}