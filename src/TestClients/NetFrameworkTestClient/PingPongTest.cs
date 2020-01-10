namespace NetFrameworkTestClient
{
    public class PingPongPayload
    {
        public int Counter { get; set; }
        public override string ToString()
        {
            return "Counter: " + Counter;
        }
    }

    public class TestInfoPayload
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public string Channel { get; set; }
        public string Reason { get; set; }
    }
}