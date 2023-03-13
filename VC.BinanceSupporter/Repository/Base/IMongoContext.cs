using System;
using MongoDB.Driver;

namespace VC.BinanceSupporter.Repository.Base
{
    public interface IMongoContext
    {
        IMongoClient Client { get; }

        string ConnectionString { get; }

        IMongoDatabase Database { get; }

        string DatabaseName { get; }
    }
}
