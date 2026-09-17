namespace SmartSolar.Api.Repositories;

public interface ICounterRepository
{
    Task<int> GetNextSequenceValueAsync(string sequenceName);
}
