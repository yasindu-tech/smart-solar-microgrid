using System.Text.RegularExpressions;
using SmartSolar.Api.DTOs.Request.Access;
using SmartSolar.Api.DTOs.Response.Access;
using SmartSolar.Api.DTOs.Response.Common;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class ProsumerService
{
    private readonly ProsumerRepository _prosumerRepo;
    public ProsumerService(ProsumerRepository prosumerRepo)
    {
        _prosumerRepo = prosumerRepo;
    }

    /// <summary>
    /// Registers a new prosumer (PendingActivation).
    /// </summary>
    public async Task<ProsumerResponse> RegisterAsync(RegisterProsumerRequest request)
    {
        var nic = request.Nic.Trim();

        // EC-1: Validate Sri Lankan NIC formats
        if (!Regex.IsMatch(nic, "^([0-9]{9}[vVxX]|[0-9]{12})$"))
            throw new DomainException("E4000", "Invalid NIC format.", 400, "nic");

        // BR-12: NIC uniqueness
        if (await _prosumerRepo.GetByNicAsync(nic) != null)
            throw new DomainException("E4001", "NIC is already registered.", 409, "nic");

        if (await _prosumerRepo.EmailExistsAsync(request.Email.Trim()))
            throw new DomainException("E4001", "Email is already registered.", 409, "email");

        if (request.Password.Length < 8)
            throw new DomainException("E4000", "Password must be at least 8 characters.", 400, "password");

        var prosumer = new Prosumer
        {
            Id = nic,
            Nic = nic,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            SolarCapacityKw = request.SolarCapacityKw,
            Status = "PendingActivation", // ADR-002: Not Active immediately
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _prosumerRepo.CreateAsync(prosumer);
        }
        catch (MongoDB.Driver.MongoWriteException ex) when (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
        {
            // Fallback for race conditions if exact field isn't parsed easily
            throw new DomainException("E4001", "Email or NIC is already in use.", 409, "emailOrNic");
        }

        return MapToResponse(prosumer);
    }

    /// <summary>
    /// Gets a paginated list of all prosumers, with optional filtering.
    /// </summary>
    public async Task<PagedResponse<ProsumerResponse>> GetAllAsync(
        string? status, string? search, int page, int pageSize)
    {
        var (items, total) = await _prosumerRepo.GetAllAsync(status, search, page, pageSize);
        return new PagedResponse<ProsumerResponse>(
            items.Select(MapToResponse).ToList(), total, page, pageSize);
    }

    /// <summary>
    /// Gets a paginated list of PendingActivation prosumers.
    /// </summary>
    public async Task<PagedResponse<ProsumerResponse>> GetPendingAsync(int page, int pageSize)
    {
        var (items, total) = await _prosumerRepo.GetPendingAsync(page, pageSize);
        return new PagedResponse<ProsumerResponse>(
            items.Select(MapToResponse).ToList(), total, page, pageSize);
    }

    /// <summary>
    /// Gets a specific prosumer by NIC, respecting RBAC access rules.
    /// </summary>
    public async Task<ProsumerResponse> GetByNicAsync(string nic, string callerNic, string callerRole)
    {
        // BR-02: Owner or Backoffice/Operator
        if (callerRole == "Prosumer" && !string.Equals(nic, callerNic, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You can only access your own profile.", 403);

        var prosumer = await _prosumerRepo.GetByNicAsync(nic);
        if (prosumer == null)
            throw new DomainException("E4404", "Prosumer not found.", 404);

        return MapToResponse(prosumer);
    }

    /// <summary>
    /// Updates a prosumer's profile details.
    /// </summary>
    public async Task<ProsumerResponse> UpdateAsync(
        string nic, UpdateProsumerRequest request, string callerNic, string callerRole)
    {
        // BR-02: Owner or Backoffice
        if (callerRole == "Prosumer" && !string.Equals(nic, callerNic, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You can only edit your own profile.", 403);

        if (callerRole == "Operator")
            throw new DomainException("E4303", "Operators cannot edit prosumer profiles.", 403);

        var prosumer = await _prosumerRepo.GetByNicAsync(nic);
        if (prosumer == null)
            throw new DomainException("E4404", "Prosumer not found.", 404);

        // EC-6: Re-check status in case Backoffice deactivated them just now.
        if (callerRole == "Prosumer" && prosumer.Status != "Active")
            throw new DomainException("E4302", "Your account is no longer active.", 403);

        if (await _prosumerRepo.EmailExistsAsync(request.Email.Trim(), nic))
            throw new DomainException("E4001", "Email is already in use.", 409, "email");

        // Notice: we do NOT update NIC (BR-12).
        prosumer.FullName = request.FullName.Trim();
        prosumer.Email = request.Email.Trim().ToLower();
        prosumer.Phone = request.Phone.Trim();
        prosumer.Address = request.Address.Trim();
        prosumer.SolarCapacityKw = request.SolarCapacityKw;
        prosumer.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _prosumerRepo.UpdateAsync(prosumer);
        }
        catch (MongoDB.Driver.MongoWriteException ex) when (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
        {
            throw new DomainException("E4001", "Email is already in use.", 409, "email");
        }

        return MapToResponse(prosumer);
    }

    /// <summary>
    /// Approves or reactivates a prosumer (Backoffice only).
    /// </summary>
    public async Task ActivateAsync(string nic, string callerRole, string callerUserId)
    {
        // BR-03: ONLY Backoffice can reactivate an account.
        if (callerRole != "Backoffice")
            throw new DomainException("E4303", "Only Backoffice administrators can approve or reactivate accounts.", 403);

        var prosumer = await _prosumerRepo.GetByNicAsync(nic);
        if (prosumer == null)
            throw new DomainException("E4404", "Prosumer not found.", 404);

        var wasDeactivated = prosumer.Status == "Deactivated";

        prosumer.Status = "Active";
        prosumer.UpdatedAt = DateTime.UtcNow;

        if (wasDeactivated)
        {
            prosumer.ReactivatedBy = callerUserId;
            // Cancelled reservations are NOT restored per UF-06 Part B.
        }

        await _prosumerRepo.UpdateAsync(prosumer);
    }

    /// <summary>
    /// Deactivates a prosumer and cascades cancellation of future reservations.
    /// </summary>
    public async Task DeactivateAsync(string nic, string callerNic, string callerRole)
    {
        // BR-02: Owner or Backoffice
        if (callerRole == "Prosumer" && !string.Equals(nic, callerNic, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You can only deactivate your own account.", 403);

        if (callerRole == "Operator")
            throw new DomainException("E4303", "Operators cannot deactivate prosumer accounts.", 403);

        var prosumer = await _prosumerRepo.GetByNicAsync(nic);
        if (prosumer == null)
            throw new DomainException("E4404", "Prosumer not found.", 404);

        if (prosumer.Status == "Deactivated") return; // Idempotent

        prosumer.Status = "Deactivated";
        prosumer.DeactivationRequestedAt = DateTime.UtcNow;
        prosumer.UpdatedAt = DateTime.UtcNow;

        await _prosumerRepo.UpdateAsync(prosumer);

        // BR-04 CASCADE: Cancel all future Pending/Approved reservations.
        // TODO (M3): Call ReservationService to cancel all future Pending/Approved reservations.
    }


    /// <summary>
    /// Maps a Prosumer document to a DTO.
    /// </summary>
    private static ProsumerResponse MapToResponse(Prosumer p)
    {
        return new ProsumerResponse(
            p.Nic,
            p.FullName,
            p.Email,
            p.Phone,
            p.Address,
            p.SolarCapacityKw,
            p.Status,
            p.DeactivationRequestedAt,
            p.ReactivatedBy,
            p.CreatedAt,
            p.UpdatedAt
        );
    }
}
