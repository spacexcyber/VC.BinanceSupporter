using MongoDB.Bson.Serialization.Attributes;
using VC.BinanceSupporter.Model.Base;

namespace VC.BinanceSupporter.Model
{
    public class PlaceOrder : BaseCollection
    {
        /// <summary>
        /// The symbol the order is for
        /// </summary>
        [BsonElement("username")]
        public string UserName { get; set; }

        /// <summary>
        /// The symbol the order is for
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Pair
        /// </summary>
        [BsonElement("pair")]
        public string? Pair { get; set; }

        /// <summary>
        /// The order id as assigned by Binance
        /// </summary>
        [BsonElement("orderId")]
        public long OrderId { get; set; }
        /// <summary>
        /// The order id as assigned by the client
        /// </summary>
        [BsonElement("clientOrderId")]
        public string ClientOrderId { get; set; } = string.Empty;
        /// <summary>
        /// The price of the order
        /// </summary>
        [BsonElement("price")]
        public decimal Price { get; set; }
        /// <summary>
        /// The original quantity of the order
        /// </summary>
        [BsonElement("origQty")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// The side of the order
        /// </summary>
        [BsonElement("side")]
        public int Side { get; set; }

        /// <summary>
        /// The position side of the order
        /// </summary>
        [BsonElement("positionSide")]
        public int PositionSide { get; set; }
    }
}
