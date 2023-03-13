using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Repository.Base;

namespace VC.BinanceSupporter.Repository
{
    public class PlaceOrderRepository : BaseRepository<PlaceOrder>, IPlaceOrderRepository
    {
        public PlaceOrderRepository(IMongoContext mongoContext) : base(mongoContext)
        {
        }
    }
}
