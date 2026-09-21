// ---------------------------------------------------------------------------
//  File        : EnergyReservation.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Mongo document model representing a prosumer's energy transfer
//                reservation claim ("EnergyReservation" collection).
// ---------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

[BsonIgnoreExtraElements]
public class EnergyReservation
{
    // MongoDB auto-generated document identifier.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Unique human-readable reservation identifier, e.g. "RS-0107".
    [BsonElement("reservationId")]
    public string ReservationId { get; set; } = string.Empty;

    // Prosumer National Identity Card number (reference -> Prosumers.nic).
    [BsonElement("nic")]
    public string Nic { get; set; } = string.Empty;

    // Target microgrid node identifier (reference -> SolarStationInfo.nodeId).
    [BsonElement("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    // Associated booking slot identifier (reference -> EnergyBookingSlots.slotId).
    [BsonElement("slotId")]
    public string SlotId { get; set; } = string.Empty;

    // Denormalised slot start time in UTC. All time rules (BR-05, BR-06, BR-07) evaluate against this.
    [BsonElement("slotStartTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SlotStartTime { get; set; }

    // Denormalised slot end time in UTC. Used for lazy expiration checks.
    [BsonElement("slotEndTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SlotEndTime { get; set; }

    // Energy quantity requested for transfer in kilowatt-hours.
    [BsonElement("requestedKwh")]
    public double RequestedKwh { get; set; }

    // Lifecycle status: "Pending", "Approved", "Cancelled", "Completed", "Expired".
    [BsonElement("status")]
    public string Status { get; set; } = "Pending";

    // Signed single-use JWT QR token issued upon operator approval (ADR-003). Null otherwise.
    [BsonElement("qrToken")]
    public string? QrToken { get; set; }

    // Timestamp when the QR token was generated and issued (UTC).
    [BsonElement("qrIssuedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? QrIssuedAt { get; set; }

    // Instant when reservation was originally booked (UTC). BR-05 7-day horizon measured from here.
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    // Timestamp of the last update to this reservation (UTC).
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }

    // Timestamp when reservation was cancelled (UTC). Null if active/completed.
    [BsonElement("cancelledAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CancelledAt { get; set; }

    // Identity (NIC or staff UserId) of who cancelled the reservation.
    [BsonElement("cancelledBy")]
    public string? CancelledBy { get; set; }

    // Timestamp when the energy transfer was completed by the operator (UTC).
    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }

    // UserId of the operator who verified the QR and finalised the transfer (e.g. "USR-0002").
    [BsonElement("completedByOperatorId")]
    public string? CompletedByOperatorId { get; set; }

    // Actual delivered energy measured at the node during the completed transfer in kilowatt-hours.
    [BsonElement("deliveredKwh")]
    public double? DeliveredKwh { get; set; }
}
