using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolar.Api.Configuration;

namespace SmartSolar.Api.Repositories;

public class Counter
{
    [MongoDB.Bson.Serialization.Attributes.BsonId]
    public string Id { get; set; } = string.Empty;
    public int SequenceValue { get; set; }
}

public class CounterRepository : ICounterRepository
{
    private readonly IMongoCollection<Counter> _counters;

    // Resolve the collection once at construction; IMongoClient is thread-safe.
    public CounterRepository(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _counters = db.GetCollection<Counter>("Counters");
    }

    /// <summary>
    /// Atomically increments and returns the next sequence value for a given sequence name.
    /// </summary>
    public async Task<int> GetNextSequenceValueAsync(string sequenceName)
    {
        var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);
        var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);
        
        var options = new FindOneAndUpdateOptions<Counter>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true
        };

        var result = await _counters.FindOneAndUpdateAsync(filter, update, options);
        return result.SequenceValue;
    }
}
