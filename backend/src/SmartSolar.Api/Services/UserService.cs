using SmartSolar.Api.DTOs.Request.Access;
using SmartSolar.Api.DTOs.Response.Access;
using SmartSolar.Api.DTOs.Response.Common;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class UserService
{
    private readonly UserRepository _userRepo;
    private readonly CounterRepository _counterRepo;
    public UserService(UserRepository userRepo, CounterRepository counterRepo)
    {
        _userRepo = userRepo;
        _counterRepo = counterRepo;
    }

    /// <summary>
    /// Gets a paginated list of users, optionally filtered by role.
    /// </summary>
    public async Task<PagedResponse<UserResponse>> GetAllAsync(
        string? role, int page, int pageSize)
    {
        var (items, total) = await _userRepo.GetAllAsync(role, page, pageSize);

        var dtos = items.Select(MapToResponse).ToList();
        return new PagedResponse<UserResponse>(dtos, total, page, pageSize);
    }

    /// <summary>
    /// Gets a specific user by their User ID.
    /// </summary>
    public async Task<UserResponse> GetByIdAsync(string userId)
    {
        var user = await _userRepo.GetByUserIdAsync(userId);
        if (user == null)
            throw new DomainException("E4404", "User not found.", 404);

        return MapToResponse(user);
    }

    /// <summary>
    /// Creates a new Backoffice or Operator user.
    /// </summary>
    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // BR-12 analogue: email must be unique
        if (await _userRepo.EmailExistsAsync(request.Email.Trim()))
            throw new DomainException("E4001", "Email is already in use.", 409, "email");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new DomainException("E4000", "Password must be at least 8 characters.", 400, "password");

        if (request.Role != "Backoffice" && request.Role != "Operator")
            throw new DomainException("E4000", "Role must be Backoffice or Operator.", 400, "role");

        var assignedNodes = request.AssignedNodeIds ?? new List<string>();
        if (request.Role == "Backoffice")
        {
            assignedNodes.Clear(); // Backoffice doesn't have assigned nodes
        }
        else if (!assignedNodes.Any())
        {
             throw new DomainException("E4000", "Operator must have at least one assigned node.", 400, "assignedNodeIds");
        }

        if (request.Role == "Operator")
        {
            // TODO (M2): Validate that assigned nodes exist.
        }

        var nextIdNum = await _counterRepo.GetNextSequenceValueAsync("userid");
        var nextUserId = $"USR-{nextIdNum:D4}";

        var user = new User
        {
            UserId = nextUserId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            AssignedNodeIds = assignedNodes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _userRepo.CreateAsync(user);
        }
        catch (MongoDB.Driver.MongoWriteException ex) when (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
        {
            throw new DomainException("E4001", "Email is already in use.", 409, "email");
        }

        return MapToResponse(user);
    }

    /// <summary>
    /// Updates an existing user's details and role.
    /// </summary>
    public async Task<UserResponse> UpdateAsync(string userId, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByUserIdAsync(userId);
        if (user == null)
            throw new DomainException("E4404", "User not found.", 404);

        if (await _userRepo.EmailExistsAsync(request.Email.Trim(), userId))
            throw new DomainException("E4001", "Email is already in use.", 409, "email");

        if (request.Role != "Backoffice" && request.Role != "Operator")
            throw new DomainException("E4000", "Role must be Backoffice or Operator.", 400, "role");

        // Guard: Cannot change the last active Backoffice to Operator
        if (user.Role == "Backoffice" && request.Role == "Operator" && user.IsActive)
        {
            if (await _userRepo.CountActiveBackofficeAsync() <= 1)
                throw new DomainException("E4303", "Cannot change the role of the final active Backoffice account.", 403);
        }

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim().ToLower();
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8)
                throw new DomainException("E4000", "Password must be at least 8 characters.", 400, "password");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        var assignedNodes = request.AssignedNodeIds ?? new List<string>();
        if (request.Role == "Backoffice")
        {
            assignedNodes.Clear(); // REQ-USER AC-7
        }
        else if (!assignedNodes.Any())
        {
            throw new DomainException("E4000", "Operator must have at least one assigned node.", 400, "assignedNodeIds");
        }

        if (request.Role == "Operator")
        {
            // TODO (M2): Validate that assigned nodes exist.
        }
        user.AssignedNodeIds = assignedNodes;

        try
        {
            await _userRepo.UpdateAsync(user);
        }
        catch (MongoDB.Driver.MongoWriteException ex) when (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
        {
            throw new DomainException("E4001", "Email is already in use.", 409, "email");
        }

        return MapToResponse(user);
    }

    /// <summary>
    /// Activates or deactivates a user account.
    /// </summary>
    public async Task SetStatusAsync(string userId, bool isActive, string callerUserId)
    {
        var user = await _userRepo.GetByUserIdAsync(userId);
        if (user == null)
            throw new DomainException("E4404", "User not found.", 404);

        // EC-1: Cannot deactivate yourself
        if (!isActive && userId == callerUserId)
            throw new DomainException("E4303", "You cannot deactivate your own account.", 403);

        // EC-2: Cannot deactivate the last remaining active Backoffice
        if (!isActive && user.Role == "Backoffice")
            if (await _userRepo.CountActiveBackofficeAsync() <= 1)
                throw new DomainException("E4303", "Cannot deactivate the final active Backoffice account.", 403);

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
    }

    /// <summary>
    /// Soft deletes a user account by deactivating it.
    /// </summary>
    public Task DeleteAsync(string userId, string callerUserId)
    {
        // Soft delete is exactly the same as deactivating.
        return SetStatusAsync(userId, false, callerUserId);
    }

    /// <summary>
    /// Maps a User document to a DTO.
    /// </summary>
    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role,
            user.AssignedNodeIds,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}
