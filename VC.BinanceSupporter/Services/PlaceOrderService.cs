using MongoDB.Driver;
using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Repository;

namespace VC.BinanceSupporter.Services
{
    public class PlaceOrderService : IPlaceOrderService
    {
        private readonly IPlaceOrderRepository _placeOrderRepository;

        public PlaceOrderService(IPlaceOrderRepository placeOrderRepository)
        {
            _placeOrderRepository = placeOrderRepository;
        }
        public PlaceOrder? GetOrder(long orderId, string userName)
        {
            return this._placeOrderRepository.Collection.Find(x => x.OrderId == orderId && x.UserName.Equals(userName)).FirstOrDefault();
        }

        public async Task InsertOrder(PlaceOrder order)
        {
            await _placeOrderRepository.Collection.ReplaceOneAsync(x => x.OrderId == order.OrderId && x.UserName.Equals(order.UserName), order, new ReplaceOptions() { IsUpsert = true }).ConfigureAwait(false);
        }

        public async Task DeleteOrder(PlaceOrder order)
        {
            await _placeOrderRepository.Collection.DeleteOneAsync(x => x.OrderId == order.OrderId && x.UserName.Equals(order.UserName)).ConfigureAwait(false);
        }

        public async Task<List<PlaceOrder>> DeleteOrders(string symbol, string userName)
        {
            var orders = _placeOrderRepository.Collection.Find(x => x.Symbol.Equals(symbol) && x.UserName.Equals(userName)).ToList();
            await _placeOrderRepository.Collection.DeleteManyAsync(x => x.Symbol.Equals(symbol) && x.UserName.Equals(userName)).ConfigureAwait(false);
            return orders;
        }
    }
}
