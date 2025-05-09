namespace PulseTune.Backend.Models
{
    public class NetworkUsage
    {
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public double BytesPerSecond { get; set; }
    }
}
