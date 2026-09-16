// ---------------------------------------------------------------------------
//  File        : UserService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Services
//  Description : Business logic for managing web accounts (Backoffice and
//                Operator). Includes self-deactivation guards and last
//                active backoffice guards (REQ-USER EC-1, EC-2).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Common;
using SmartSolar.Api.DTOs.Users;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class UserService
{
    private readonly UserRepository _userRepo;

    public UserService(UserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<PagedResponse<UserResponse>> GetAllAsync(
        string? role, int page, int pageSize)
    {
        var (items, total) = await _userRepo.GetAllAsync(role, page, pageSize);

        var dtos = items.Select(MapToResponse).ToList();
        return new PagedResponse<UserResponse>(dtos, total, page, pageSize);
    }

    public async Task<UserResponse> GetByIdAsync(string userId)
    {
        var user = await _userRepo.GetByUserIdAsync(userId);
        if (user == null)
            throw new DomainException("E4404", "User not found.", 404);

        return MapToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // BR-12 analogue: email must be unique
        if (await _userRepo.EmailExistsAsync(request.Email.Trim()))
            throw new DomainException("E4001", "Email is already in use.", 409, "email");

        if (request.Password.Length < 8)
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
             // For Operators, at least one node is typically expected, but if business rules don't strictly enforce >0, we proceed.
             // Usually, validation against SolarStationInfo would happen here to ensure node exists (E4404).
             // Since D2 (Inventory) owns nodes, we'll assume a basic check would go here if we had the NodeRepo.
        }

        var nextUserId = await _userRepo.GetNextUserIdAsync();

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

        await _userRepo.CreateAsync(user);

        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(string userId, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByUserIdAsync(userId);
        if (user == null)
            throw new DomainException("E4404", "User not found.", 404);

        if (await _userRepo.EmailExistsAsync(request.Email.Trim(), userId))
            throw new DomainException("E4001", "Email is already in use.", 409, "email");

        if (request.Role != "Backoffice" && request.Role != "Operator")
            throw new DomainException("E4000", "Role must be Backoffice or Operator.", 400, "role");

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim().ToLower();
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        if (request.Password != null)
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
        user.AssignedNodeIds = assignedNodes;

        await _userRepo.UpdateAsync(user);

        return MapToResponse(user);
    }

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

    public Task DeleteAsync(string userId, string callerUserId)
    {
        // Soft delete is exactly the same as deactivating.
        return SetStatusAsync(userId, false, callerUserId);
    }

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
