using MongoDB.Driver;
using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Repository;

namespace VC.BinanceSupporter.Services
{
    public class BinanceConfigService : IBinanceConfigService
    {
        private readonly IBinanceConfigRepository _binanceConfigRepository;

        public BinanceConfigService(IBinanceConfigRepository binanceConfigRepository)
        {
            _binanceConfigRepository = binanceConfigRepository;
        }
        public BinanceConfig? GetConfig(string name)
        {
            return this._binanceConfigRepository.Collection.Find(x => x.Name.Equals(name)).FirstOrDefault();
        }

        public async Task InsertConfig(BinanceConfig config)
        {
            await _binanceConfigRepository.Collection.ReplaceOneAsync(x => x.Name.Equals(config.Name), config, new ReplaceOptions() { IsUpsert = true }).ConfigureAwait(false);
        }
    }
}
