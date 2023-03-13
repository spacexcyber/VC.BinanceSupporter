using VC.BinanceSupporter.Model;

namespace VC.BinanceSupporter.Services
{
    public interface IBinanceConfigService
    {
        Task InsertConfig(BinanceConfig config);
        BinanceConfig? GetConfig(string name);
    }
}
