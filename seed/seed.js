// ---------------------------------------------------------------------------
//  seed.js - sample data for the demo and the viva
//
//  Usage:  mongosh "<connection-string>/smartsolar" seed/seed.js
//
//  Creates every status at least once so each business rule can be shown
//  working AND failing. See docs/architecture/data-model.md section 4.
//  Web passwords: Nimal123, Sunil123, Ruwan123
//  Prosumer passwords: Kamal123, Saman123, Dilani123, Anura123
// ---------------------------------------------------------------------------

const now = new Date();
const hours = (n) => new Date(now.getTime() + n * 3600 * 1000);
const days = (n) => new Date(now.getTime() + n * 24 * 3600 * 1000);

db.Users.deleteMany({});
db.Prosumers.deleteMany({});
db.SolarStationInfo.deleteMany({});
db.EnergyBookingSlots.deleteMany({});
db.EnergyReservation.deleteMany({});

// --- Web accounts: one back-office officer, two operators -------------------
// Passwords: Nimal123, Sunil123, Ruwan123
db.Users.insertMany([
  { userId: "USR-0001", fullName: "Nimal Perera", email: "backoffice@grid.lk", passwordHash: "$2a$11$UvzNzk/nb.8MrRuH0BZLj.wour.S7NXZ2V1LjKtKZHthQuNY5xPQq", role: "Backoffice", assignedNodeIds: [], isActive: true, createdAt: now, updatedAt: now },
  { userId: "USR-0002", fullName: "Sunil Fernando", email: "op1@grid.lk", passwordHash: "$2a$11$fdeYPMWH4e..TeVyq3DCOugPZxrZ5VTFBdJ8ph6PEE3rW1mRo1mpy", role: "Operator", assignedNodeIds: ["ND-001"], isActive: true, createdAt: now, updatedAt: now },
  { userId: "USR-0003", fullName: "Ruwan Jayasuriya", email: "op2@grid.lk", passwordHash: "$2a$11$vhLULSYFDVxW.HkpL1OZheSqw0nzAu7px1aEDw8PhE1FARUq0gQaS", role: "Operator", assignedNodeIds: ["ND-002"], isActive: true, createdAt: now, updatedAt: now }
]);

// --- Prosumers: every account status represented ----------------------------
// Passwords: Kamal123, Saman123, Dilani123, Anura123
db.Prosumers.insertMany([
  { _id: "199512345678", nic: "199512345678", fullName: "Kamal Silva", email: "kamal@mail.com", phone: "0771234567", address: "12 Galle Rd, Colombo 03", passwordHash: "$2a$11$HZUiQ8HLAkev0f8HjjI8BueVRDD1BE0xrYgwBuWb74vET7KNeHDGm", solarCapacityKw: 5.5, status: "Active", deactivationRequestedAt: null, reactivatedBy: null, createdAt: now, updatedAt: now },
  { _id: "199887654321", nic: "199887654321", fullName: "Saman Wijesinghe", email: "saman@mail.com", phone: "0719876543", address: "44 Kandy Rd, Kadawatha", passwordHash: "$2a$11$d/EFr0DYgJHGz/ad4R./jOFzmWjA6eNDOI4Lx7Ph2HOJqjJwZ6f3q", solarCapacityKw: 3.0, status: "Active", deactivationRequestedAt: null, reactivatedBy: null, createdAt: now, updatedAt: now },
  { _id: "200012349876", nic: "200012349876", fullName: "Dilani Rathnayake", email: "dilani@mail.com", phone: "0765554443", address: "7 Lake Dr, Kotte", passwordHash: "$2a$11$wJ.pin.p3SQmCIwqYtF/Vu2Fyss6bMAmAAsD1WW3ca9NHcBe0VhLq", solarCapacityKw: 7.2, status: "PendingActivation", deactivationRequestedAt: null, reactivatedBy: null, createdAt: now, updatedAt: now },
  { _id: "197744556677", nic: "197744556677", fullName: "Anura Bandara", email: "anura@mail.com", phone: "0701112223", address: "90 Main St, Moratuwa", passwordHash: "$2a$11$5sLtehSojp.tt64TIsHi5OERemGpKhW.5cZYdotUhYtyujxJISkNm", solarCapacityKw: 4.1, status: "Deactivated", deactivationRequestedAt: days(-3), reactivatedBy: null, createdAt: now, updatedAt: now }
]);

// --- Microgrid hubs: real Colombo-area coordinates so the map looks right ---
db.SolarStationInfo.insertMany([
  { nodeId: "ND-001", name: "Moratuwa Microgrid Hub A", location: { latitude: 6.7729, longitude: 79.8816, addressLine: "Katubedda, Moratuwa" }, capacityKwh: 250.0, batterySlotCount: 8, type: "AC", schedule: [{ dayOfWeek: "Mon", openTime: "06:00", closeTime: "18:00" }], isActive: true, createdBy: "USR-0001", createdAt: now, updatedAt: now },
  { nodeId: "ND-002", name: "Colombo 07 Hub", location: { latitude: 6.9061, longitude: 79.8612, addressLine: "Cinnamon Gardens, Colombo 07" }, capacityKwh: 180.0, batterySlotCount: 6, type: "DC", schedule: [{ dayOfWeek: "Mon", openTime: "07:00", closeTime: "20:00" }], isActive: true, createdBy: "USR-0001", createdAt: now, updatedAt: now },
  { nodeId: "ND-003", name: "Kadawatha Hub", location: { latitude: 7.0031, longitude: 79.9503, addressLine: "Kadawatha Junction" }, capacityKwh: 120.0, batterySlotCount: 4, type: "AC", schedule: [{ dayOfWeek: "Mon", openTime: "08:00", closeTime: "17:00" }], isActive: true, createdBy: "USR-0001", createdAt: now, updatedAt: now }
]);

// --- Slots. SL-0001 starts in 6 hours so the 12-hour rule can be demonstrated
//     failing on a real record (BR-06 / BR-07).
db.EnergyBookingSlots.insertMany([
  { slotId: "SL-0001", nodeId: "ND-001", startTime: hours(6), endTime: hours(8), totalCapacity: 4, availableCapacity: 3, energyOfferedKwh: 20.0, isActive: true, createdAt: now, updatedAt: now },
  { slotId: "SL-0002", nodeId: "ND-001", startTime: days(2), endTime: hours(50), totalCapacity: 4, availableCapacity: 3, energyOfferedKwh: 20.0, isActive: true, createdAt: now, updatedAt: now },
  { slotId: "SL-0003", nodeId: "ND-002", startTime: days(3), endTime: hours(74), totalCapacity: 2, availableCapacity: 0, energyOfferedKwh: 25.0, isActive: true, createdAt: now, updatedAt: now },
  { slotId: "SL-0004", nodeId: "ND-002", startTime: days(6), endTime: hours(146), totalCapacity: 4, availableCapacity: 4, energyOfferedKwh: 25.0, isActive: true, createdAt: now, updatedAt: now },
  { slotId: "SL-0005", nodeId: "ND-003", startTime: days(-2), endTime: hours(-46), totalCapacity: 4, availableCapacity: 3, energyOfferedKwh: 15.0, isActive: true, createdAt: now, updatedAt: now }
]);

// --- Reservations: one of every status ---------------------------------------
db.EnergyReservation.insertMany([
  { reservationId: "RS-0001", nic: "199512345678", nodeId: "ND-001", slotId: "SL-0002", slotStartTime: days(2), slotEndTime: hours(50), requestedKwh: 15.0, status: "Pending", qrToken: null, qrIssuedAt: null, createdAt: now, updatedAt: now, cancelledAt: null, cancelledBy: null, completedAt: null, completedByOperatorId: null, deliveredKwh: null },
  { reservationId: "RS-0002", nic: "199512345678", nodeId: "ND-001", slotId: "SL-0001", slotStartTime: hours(6), slotEndTime: hours(8), requestedKwh: 10.0, status: "Approved", qrToken: "<token>", qrIssuedAt: now, createdAt: now, updatedAt: now, cancelledAt: null, cancelledBy: null, completedAt: null, completedByOperatorId: null, deliveredKwh: null },
  { reservationId: "RS-0003", nic: "199887654321", nodeId: "ND-002", slotId: "SL-0003", slotStartTime: days(3), slotEndTime: hours(74), requestedKwh: 20.0, status: "Approved", qrToken: "<token>", qrIssuedAt: now, createdAt: now, updatedAt: now, cancelledAt: null, cancelledBy: null, completedAt: null, completedByOperatorId: null, deliveredKwh: null },
  { reservationId: "RS-0004", nic: "199887654321", nodeId: "ND-003", slotId: "SL-0005", slotStartTime: days(-2), slotEndTime: hours(-46), requestedKwh: 12.0, status: "Completed", qrToken: null, qrIssuedAt: null, createdAt: days(-4), updatedAt: now, cancelledAt: null, cancelledBy: null, completedAt: days(-2), completedByOperatorId: "USR-0003", deliveredKwh: 11.6 },
  { reservationId: "RS-0005", nic: "199512345678", nodeId: "ND-002", slotId: "SL-0004", slotStartTime: days(6), slotEndTime: hours(146), requestedKwh: 18.0, status: "Cancelled", qrToken: null, qrIssuedAt: null, createdAt: days(-1), updatedAt: now, cancelledAt: days(-1), cancelledBy: "199512345678", completedAt: null, completedByOperatorId: null, deliveredKwh: null }
]);

// --- Indexes the data model requires -----------------------------------------
db.Users.createIndex({ email: 1 }, { unique: true });
db.Users.createIndex({ userId: 1 }, { unique: true });
db.Prosumers.createIndex({ email: 1 }, { unique: true });
db.Prosumers.createIndex({ nic: 1 }, { collation: { locale: "en", strength: 2 } });
db.Prosumers.createIndex({ status: 1 });
db.SolarStationInfo.createIndex({ nodeId: 1 }, { unique: true });
db.SolarStationInfo.createIndex({ isActive: 1 });
db.EnergyBookingSlots.createIndex({ slotId: 1 }, { unique: true });
db.EnergyBookingSlots.createIndex({ nodeId: 1, startTime: 1 });
db.EnergyReservation.createIndex({ reservationId: 1 }, { unique: true });
db.EnergyReservation.createIndex({ nic: 1, status: 1 });
db.EnergyReservation.createIndex({ nodeId: 1, slotStartTime: 1 });

print("Seeded: " +
  db.Users.countDocuments() + " users, " +
  db.Prosumers.countDocuments() + " prosumers, " +
  db.SolarStationInfo.countDocuments() + " nodes, " +
  db.EnergyBookingSlots.countDocuments() + " slots, " +
  db.EnergyReservation.countDocuments() + " reservations");
