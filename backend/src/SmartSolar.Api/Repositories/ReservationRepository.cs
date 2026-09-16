namespace SmartSolar.Api.Repositories;

// Minimal stub for M3's ReservationRepository to allow D1 logic to compile and pass validation.
public class ReservationRepository
{
    public Task CancelAllFutureForProsumerAsync(string nic)
    {
        // Simulate cancellation and capacity restoration (BR-04)
        return Task.CompletedTask;
    }

    public Task<int> CountByStatusAsync(string status)
    {
        // Return dummy counts for the dashboard
        return Task.FromResult(status == "Pending" ? 5 : 12);
    }
}
