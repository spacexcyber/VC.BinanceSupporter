namespace VC.BinanceSupporter.Model
{
    public class CancelParam : ApiModel
    {
        public string symbol { get; set; }

        public long? orderId { get; set; } = null;
    }
}
