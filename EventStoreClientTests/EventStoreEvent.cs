namespace EventStoreClientTests
{
    public class EventStoreEvent
    {
        public byte[] Data { get; internal set; }
        public string EventStreamId { get; internal set; }
        public string EventType { get; internal set; }
    }
}