namespace SmartSolar.Api.Repositories;

public interface ICounterRepository
{
    /// <summary>
    /// Atomically increment and return the next value for a named sequence.
    /// Must be a single findAndModify with upsert - never read-then-write,
    /// which races under concurrent creates.
    /// </summary>
    Task<int> GetNextSequenceValueAsync(string sequenceName);
}
