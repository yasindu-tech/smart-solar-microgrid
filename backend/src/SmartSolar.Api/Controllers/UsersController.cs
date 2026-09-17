using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Access;
using SmartSolar.Api.DTOs.Response.Access;
using SmartSolar.Api.DTOs.Response.Common;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Backoffice")] // BR-01: Proves the rule is in the API
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets a paginated list of all web users.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = await _userService.GetAllAsync(role, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific web user by their ID.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetById(string userId)
    {
        var response = await _userService.GetByIdAsync(userId);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new Backoffice or Operator user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var response = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { userId = response.UserId }, response);
    }

    /// <summary>
    /// Updates an existing web user's profile and role.
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(string userId, [FromBody] UpdateUserRequest request)
    {
        var response = await _userService.UpdateAsync(userId, request);
        return Ok(response);
    }

    /// <summary>
    /// Activates or deactivates a web user.
    /// </summary>
    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> SetStatus(string userId, [FromBody] UserStatusRequest request)
    {
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub") ?? string.Empty;

        await _userService.SetStatusAsync(userId, request.IsActive, callerUserId);
        return Ok();
    }

    /// <summary>
    /// Soft deletes a web user by deactivating them.
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(string userId)
    {
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub") ?? string.Empty;

        await _userService.DeleteAsync(userId, callerUserId);
        return Ok();
    }
}
