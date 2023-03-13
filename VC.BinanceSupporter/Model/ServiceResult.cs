namespace VC.BinanceSupporter.Model
{
    public class ServiceResult
    {
        public int status { get; set; } = 1;

        public object data { get; set; }

        public string message { get; set; }
        public string code { get; set; }
    }
}
