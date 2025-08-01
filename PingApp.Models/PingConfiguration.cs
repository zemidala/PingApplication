namespace PingApp.Models
{
    public class PingConfiguration
    {
        public string Host { get; set; }
        public int PacketSize { get; set; } = 32;
        public int Interval { get; set; } = 1000; // ms
        public int Timeout { get; set; } = 5000; // ms
    }
}