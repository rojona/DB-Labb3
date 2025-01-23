using MongoDB.Driver;

namespace NET24_Labb2_WPF.Database;

public class MongoDBService : IDisposable
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDBService(MongoDBSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    public void Dispose()
    {
        _client?.Cluster?.Dispose();
    }
}