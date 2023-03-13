using Binance.Net.Enums;

namespace VC.BinanceSupporter.Model
{
    public class OrderParam : ApiModel
    {
        public string symbol { get; set; }
        public decimal quantity { get; set; }
        public decimal price { get; set; }
        public OrderSide side { get; set; } = OrderSide.Buy;
        public FuturesOrderType type { get; set; } = FuturesOrderType.Limit;
        public decimal? stoploss { get; set; } = null;
        public decimal? takeprofit { get; set; } = null;
        public bool market { get; set; }
    }
}
