using VC.BinanceSupporter.Model;

namespace VC.BinanceSupporter.Services
{
    public interface IPlaceOrderService
    {
        Task InsertOrder(PlaceOrder config);
        Task DeleteOrder(PlaceOrder config);
        Task<List<PlaceOrder>> DeleteOrders(string symbol, string userName);
        PlaceOrder? GetOrder(long orderId, string userName);
    }
}