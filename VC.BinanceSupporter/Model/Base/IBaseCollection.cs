using System;
namespace VC.BinanceSupporter.Model.Base
{
    public interface IBaseCollection<TKey> where TKey : IEquatable<TKey>
    {
        TKey id { get; set; }
        int version { get; set; }
    }

    public interface IBaseCollection : IBaseCollection<string>
    {

    }
}
