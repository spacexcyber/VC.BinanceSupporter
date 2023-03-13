using VC.BinanceSupporter.Model.Base;

namespace VC.BinanceSupporter.Model
{
    public class BinanceConfig : BaseCollection
    {
        public string Name { get; set; }

        public string Key { get; set; }

        public string Secret { get; set; }
        public long MaxVol { get; set; }

        public string EncryptionKey { get; set; }
    }
}
