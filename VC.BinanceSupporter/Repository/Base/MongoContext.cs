using System;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace VC.BinanceSupporter.Repository.Base
{
    public class MongoContext : IMongoContext
    {
        private readonly IMongoClient _client;
        public IMongoClient Client => _client;

        private readonly string _connectionString;
        public string ConnectionString => _connectionString;

        private readonly IMongoDatabase _database;
        public IMongoDatabase Database => _database;

        private readonly string _databaseName;
        public string DatabaseName => _databaseName;

        public MongoContext(IConfiguration configuration)
        {
            this._connectionString = configuration.GetConnectionString("MongoConnection");
            if (!string.IsNullOrEmpty(this._connectionString))
            {
                this._databaseName = MongoUrl.Create(this._connectionString).DatabaseName;
                this._client = new MongoClient(this._connectionString);
                this._database = this._client.GetDatabase(this._databaseName);
            }
        }
    }
}
