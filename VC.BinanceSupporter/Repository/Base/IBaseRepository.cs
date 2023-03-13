using MongoDB.Driver;
using VC.BinanceSupporter.Model.Base;

namespace VC.BinanceSupporter.Repository.Base
{
    public interface IBaseRepository<TBaseCollection> where TBaseCollection : IBaseCollection
    {
        IMongoCollection<TBaseCollection> Collection { get; }

        Task InsertOneAsync(TBaseCollection baseCollection);

        Task InsertManyAsync(List<TBaseCollection> baseCollection);

        Task DeleteOneAsync(TBaseCollection baseCollection);

        Task UpdateOneAsync(TBaseCollection baseCollection);
    }
}
