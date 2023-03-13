using MongoDB.Driver;
using VC.BinanceSupporter.Model.Base;

namespace VC.BinanceSupporter.Repository.Base
{
    public class BaseRepository<TBaseCollection> : IBaseRepository<TBaseCollection> where TBaseCollection : IBaseCollection
    {
        private readonly IMongoCollection<TBaseCollection> _collection;
        public IMongoCollection<TBaseCollection> Collection => _collection;

        public BaseRepository(IMongoContext mongoContext)
        {
            this._collection = mongoContext.Database.GetCollection<TBaseCollection>(typeof(TBaseCollection).Name);
        }

        public async Task InsertOneAsync(TBaseCollection baseCollection)
        {
            await this.Collection.InsertOneAsync(baseCollection).ConfigureAwait(false);
        }

        public async Task InsertManyAsync(List<TBaseCollection> baseCollection)
        {
            await this.Collection.InsertManyAsync(baseCollection).ConfigureAwait(false);
        }

        public async Task DeleteOneAsync(TBaseCollection baseCollection)
        {
            await this.Collection.DeleteOneAsync(x => x.id == baseCollection.id).ConfigureAwait(false);
        }

        public async Task UpdateOneAsync(TBaseCollection baseCollection)
        {
            await this.Collection.ReplaceOneAsync(x => x.id == baseCollection.id, baseCollection, new ReplaceOptions() { IsUpsert = true }).ConfigureAwait(false);
        }
    }
}
