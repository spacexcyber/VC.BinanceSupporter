using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Repository.Base;

namespace VC.BinanceSupporter.Repository
{
    public class BinanceConfigRepository : BaseRepository<BinanceConfig>, IBinanceConfigRepository
    {
        public BinanceConfigRepository(IMongoContext mongoContext) : base(mongoContext)
        {
        }
    }
}
