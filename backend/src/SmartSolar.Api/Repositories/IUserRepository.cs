// ---------------------------------------------------------------------------
//  File        : IUserRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Data-access contract for the "Users" collection.
// ---------------------------------------------------------------------------

using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByUserIdAsync(string userId);

    Task<(List<User> Items, int Total)> GetAllAsync(string? role, int page, int pageSize);

    Task CreateAsync(User user);

    Task UpdateAsync(User user);

    Task<int> CountActiveBackofficeAsync();

    Task<bool> EmailExistsAsync(string email, string? excludeUserId = null);
}
