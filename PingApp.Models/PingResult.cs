namespace PingApp.Models;

public class PingResult
{
    public bool IsSuccess { get; set; }
    public long RoundTripTime { get; set; }
    public string Host { get; set; }
    public DateTime Timestamp { get; set; }
    public int PacketSize { get; set; }
    public string ErrorMessage { get; set; }
    public string IpAddress { get; set; }
}