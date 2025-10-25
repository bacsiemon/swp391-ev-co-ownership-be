# EV Co-Ownership System
## Master Database Design Document

---

## 1. Overview
This document provides a unified, normalized, and implementation-accurate design of the EV Co-Ownership system database. It includes:
- Table/entity definitions
- Attribute lists and types
- Primary and foreign keys
- Relationship mapping
- Special constraints and notes

---

## 2. Entity/Table List

### 2.1. User
- **Id** (PK, int)
- Email (string, unique)
- NormalizedEmail (string)
- PasswordHash (string)
- PasswordSalt (string)
- FirstName (string)
- LastName (string)
- Phone (string)
- DateOfBirth (DateOnly)
- Address (string)
- ProfileImageUrl (string)
- RoleEnum (EUserRole, int/enum)
- StatusEnum (EUserStatus, int/enum)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.2. CoOwner
- **UserId** (PK, FK to User.Id)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.3. Vehicle
- **Id** (PK, int)
- Name (string)
- Description (string)
- Brand (string)
- Model (string)
- Year (int)
- Vin (string, unique)
- LicensePlate (string, unique)
- Color (string)
- BatteryCapacity (decimal)
- RangeKm (int)
- PurchaseDate (DateOnly)
- PurchasePrice (decimal)
- WarrantyUntil (DateOnly)
- DistanceTravelled (int)
- StatusEnum (EVehicleStatus, int/enum)
- VerificationStatusEnum (EVehicleVerificationStatus, int/enum)
- LocationLatitude (decimal)
- LocationLongitude (decimal)
- CreatedBy (FK to User.Id)
- FundId (FK to Fund.Id)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.4. VehicleCoOwner
- **CoOwnerId** (PK, FK to CoOwner.UserId)
- **VehicleId** (PK, FK to Vehicle.Id)
- OwnershipPercentage (decimal)
- InvestmentAmount (decimal)
- StatusEnum (EContractStatus, int/enum)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.5. Booking
- **Id** (PK, int)
- CoOwnerId (FK to CoOwner.UserId)
- VehicleId (FK to Vehicle.Id)
- StartTime (DateTime)
- EndTime (DateTime)
- Purpose (string)
- StatusEnum (EBookingStatus, int/enum)
- ApprovedBy (FK to User.Id)
- TotalCost (decimal)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.6. CheckIn
- **Id** (PK, int)
- BookingId (FK to Booking.Id)
- StaffId (FK to User.Id)
- VehicleStationId (FK to VehicleStation.Id)
- VehicleConditionId (FK to VehicleCondition.Id)
- CheckTime (DateTime)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.7. CheckOut
- **Id** (PK, int)
- BookingId (FK to Booking.Id)
- StaffId (FK to User.Id)
- VehicleStationId (FK to VehicleStation.Id)
- VehicleConditionId (FK to VehicleCondition.Id)
- CheckTime (DateTime)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.8. VehicleCondition
- **Id** (PK, int)
- VehicleId (FK to Vehicle.Id)
- ReportedBy (FK to User.Id)
- ConditionTypeEnum (EVehicleConditionType, int/enum)
- Description (string)
- PhotoUrls (string)
- OdometerReading (int)
- FuelLevel (decimal)
- DamageReported (bool)
- CreatedAt (DateTime)

### 2.9. Fund
- **Id** (PK, int)
- CurrentBalance (decimal)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.10. FundAddition
- **Id** (PK, int)
- FundId (FK to Fund.Id)
- CoOwnerId (FK to CoOwner.UserId)
- Amount (decimal)
- PaymentMethodEnum (EPaymentMethod, int/enum)
- TransactionId (string)
- Description (string)
- StatusEnum (EFundAdditionStatus, int/enum)
- CreatedAt (DateTime)

### 2.11. FundUsage
- **Id** (PK, int)
- FundId (FK to Fund.Id)
- UsageTypeEnum (EUsageType, int/enum)
- Amount (decimal)
- Description (string)
- ImageUrl (string)
- MaintenanceCostId (FK to MaintenanceCost.Id)
- CreatedAt (DateTime)

### 2.12. FundUsageVote
- **FundUsageId** (PK, FK to FundUsage.Id)
- **UserId** (PK, FK to User.Id)
- IsAgree (bool)
- CreatedAt (DateTime)

### 2.13. MaintenanceCost
- **Id** (PK, int)
- VehicleId (FK to Vehicle.Id)
- BookingId (FK to Booking.Id)
- MaintenanceTypeEnum (EMaintenanceType, int/enum)
- Description (string)
- Cost (decimal)
- IsPaid (bool)
- ServiceProvider (string)
- ServiceDate (DateOnly)
- OdometerReading (int)
- ImageUrl (string)
- CreatedAt (DateTime)

### 2.14. Payment
- **Id** (PK, int)
- UserId (FK to User.Id)
- Amount (decimal)
- TransactionId (string)
- PaymentGateway (string)
- StatusEnum (EPaymentStatus, int/enum)
- PaidAt (DateTime)
- CreatedAt (DateTime)
- FundAdditionId (FK to FundAddition.Id)

### 2.15. NotificationEntity
- **Id** (PK, int)
- NotificationType (string)
- AdditionalDataJson (string)
- CreatedAt (DateTime)

### 2.16. UserNotification
- **Id** (PK, int)
- NotificationId (FK to NotificationEntity.Id)
- UserId (FK to User.Id)
- ReadAt (DateTime)

### 2.17. UserRefreshToken
- **UserId** (PK, FK to User.Id)
- RefreshToken (string)
- ExpiresAt (DateTime)

### 2.18. VehicleVerificationHistory
- **Id** (PK, int)
- VehicleId (FK to Vehicle.Id)
- StaffId (FK to User.Id)
- StatusEnum (EVehicleVerificationStatus, int/enum)
- Notes (string)
- ImagesJson (string)
- CreatedAt (DateTime)

### 2.19. VehicleUsageRecord
- **Id** (PK, int)
- BookingId (FK to Booking.Id)
- VehicleId (FK to Vehicle.Id)
- CoOwnerId (FK to CoOwner.UserId)
- CheckInId (FK to CheckIn.Id)
- CheckOutId (FK to CheckOut.Id)
- StartTime (DateTime)
- EndTime (DateTime)
- DurationHours (decimal)
- DistanceKm (int)
- ... (other trip/battery/usage fields)

### 2.20. VehicleStation
- **Id** (PK, int)
- Name (string)
- Description (string)
- Address (string)
- ContactNumber (string)
- LocationLatitude (decimal)
- LocationLongitude (decimal)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.21. VehicleUpgradeProposal
- **Id** (PK, int)
- VehicleId (FK to Vehicle.Id)
- UpgradeType (EUpgradeType, int/enum)
- Title (string)
- Description (string)
- EstimatedCost (decimal)
- Justification (string)
- ImageUrl (string)
- VendorName (string)
- VendorContact (string)
- ProposedInstallationDate (DateTime)
- EstimatedDurationDays (int)
- ProposedByUserId (FK to User.Id)
- ProposedAt (DateTime)
- Status (string)
- ApprovedAt (DateTime)
- RejectedAt (DateTime)
- CancelledAt (DateTime)
- IsExecuted (bool)
- ExecutedAt (DateTime)
- ActualCost (decimal)
- ExecutionNotes (string)
- FundUsageId (FK to FundUsage.Id)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

#### VehicleUpgradeVote
- **ProposalId** (PK, FK to VehicleUpgradeProposal.Id)
- **UserId** (PK, FK to User.Id)
- IsAgree (bool)
- Comments (string)
- VotedAt (DateTime)

### 2.22. OwnershipChangeRequest
- **Id** (PK, int)
- VehicleId (FK to Vehicle.Id)
- ProposedByUserId (FK to User.Id)
- Reason (string)
- StatusEnum (EOwnershipChangeStatus, int/enum)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)
- FinalizedAt (DateTime)
- RequiredApprovals (int)
- CurrentApprovals (int)

### 2.23. OwnershipHistory
- **Id** (PK, int)
- VehicleId (FK to Vehicle.Id)
- CoOwnerId (FK to CoOwner.UserId)
- UserId (FK to User.Id)
- OwnershipChangeRequestId (FK to OwnershipChangeRequest.Id)
- PreviousPercentage (decimal)
- NewPercentage (decimal)
- PercentageChange (decimal)
- PreviousInvestment (decimal)
- NewInvestment (decimal)
- InvestmentChange (decimal)
- ChangedAt (DateTime)

### 2.24. Configuration
- **Key** (PK, string)
- Value (string)
- Description (string)
- UpdatedAt (DateTime)

### 2.25. UserReminderPreference
- **Id** (PK, int)
- UserId (FK to User.Id)
- HoursBeforeBooking (int)
- Enabled (bool)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### 2.26. BookingReminderLog
- **Id** (PK, int)
- BookingId (FK to Booking.Id)
- UserId (FK to User.Id)
- SentAt (DateTime)
- BookingStartTime (DateTime)
- HoursBeforeBooking (double)
- Success (bool)
- ErrorMessage (string)

### 2.27. FileUpload
- **Id** (PK, int)
- Data (byte[])
- FileName (string)
- MimeType (string)
- UploadedAt (DateTime)

### 2.28. DrivingLicense
- **Id** (PK, int)
- CoOwnerId (FK to CoOwner.UserId)
- LicenseNumber (string)
- IssuedBy (string)
- IssueDate (DateOnly)
- ExpiryDate (DateOnly)
- LicenseImageUrl (string)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

---

## 3. Relationship Mapping
- **User** 1---1 **CoOwner** (CoOwner.UserId = User.Id)
- **User** 1---* **Booking** (Booking.ApprovedBy = User.Id)
- **User** 1---* **Vehicle** (Vehicle.CreatedBy = User.Id)
- **User** 1---* **UserNotification** (UserNotification.UserId = User.Id)
- **User** 1---* **UserRefreshToken** (UserRefreshToken.UserId = User.Id)
- **User** 1---* **Payment** (Payment.UserId = User.Id)
- **User** 1---* **VehicleCondition** (VehicleCondition.ReportedBy = User.Id)
- **User** 1---* **VehicleVerificationHistory** (VehicleVerificationHistory.StaffId = User.Id)
- **User** 1---* **OwnershipChangeRequest** (OwnershipChangeRequest.ProposedByUserId = User.Id)
- **User** 1---* **OwnershipHistory** (OwnershipHistory.UserId = User.Id)
- **User** 1---* **VehicleUpgradeProposal** (VehicleUpgradeProposal.ProposedByUserId = User.Id)
- **User** 1---* **VehicleUpgradeVote** (VehicleUpgradeVote.UserId = User.Id)
- **User** 1---* **FundUsageVote** (FundUsageVote.UserId = User.Id)
- **User** 1---* **UserReminderPreference** (UserReminderPreference.UserId = User.Id)
- **User** 1---* **BookingReminderLog** (BookingReminderLog.UserId = User.Id)
- **CoOwner** 1---* **Booking** (Booking.CoOwnerId = CoOwner.UserId)
- **CoOwner** 1---* **DrivingLicense** (DrivingLicense.CoOwnerId = CoOwner.UserId)
- **CoOwner** 1---* **FundAddition** (FundAddition.CoOwnerId = CoOwner.UserId)
- **CoOwner** 1---* **VehicleCoOwner** (VehicleCoOwner.CoOwnerId = CoOwner.UserId)
- **CoOwner** 1---* **OwnershipHistory** (OwnershipHistory.CoOwnerId = CoOwner.UserId)
- **Vehicle** 1---* **Booking** (Booking.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **VehicleCoOwner** (VehicleCoOwner.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **MaintenanceCost** (MaintenanceCost.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **VehicleCondition** (VehicleCondition.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **VehicleVerificationHistory** (VehicleVerificationHistory.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **VehicleUpgradeProposal** (VehicleUpgradeProposal.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **OwnershipChangeRequest** (OwnershipChangeRequest.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **OwnershipHistory** (OwnershipHistory.VehicleId = Vehicle.Id)
- **Vehicle** 1---* **VehicleUsageRecord** (VehicleUsageRecord.VehicleId = Vehicle.Id)
- **Fund** 1---* **FundAddition** (FundAddition.FundId = Fund.Id)
- **Fund** 1---* **FundUsage** (FundUsage.FundId = Fund.Id)
- **Fund** 1---* **Vehicle** (Vehicle.FundId = Fund.Id)
- **FundUsage** 1---* **FundUsageVote** (FundUsageVote.FundUsageId = FundUsage.Id)
- **FundUsage** 1---1 **VehicleUpgradeProposal** (VehicleUpgradeProposal.FundUsageId = FundUsage.Id)
- **FundAddition** 1---* **Payment** (Payment.FundAdditionId = FundAddition.Id)
- **Booking** 1---* **CheckIn** (CheckIn.BookingId = Booking.Id)
- **Booking** 1---* **CheckOut** (CheckOut.BookingId = Booking.Id)
- **Booking** 1---* **MaintenanceCost** (MaintenanceCost.BookingId = Booking.Id)
- **Booking** 1---* **BookingReminderLog** (BookingReminderLog.BookingId = Booking.Id)
- **Booking** 1---* **VehicleUsageRecord** (VehicleUsageRecord.BookingId = Booking.Id)
- **CheckIn** 1---* **VehicleUsageRecord** (VehicleUsageRecord.CheckInId = CheckIn.Id)
- **CheckOut** 1---* **VehicleUsageRecord** (VehicleUsageRecord.CheckOutId = CheckOut.Id)
- **VehicleStation** 1---* **CheckIn** (CheckIn.VehicleStationId = VehicleStation.Id)
- **VehicleStation** 1---* **CheckOut** (CheckOut.VehicleStationId = VehicleStation.Id)
- **NotificationEntity** 1---* **UserNotification** (UserNotification.NotificationId = NotificationEntity.Id)

---

## 4. Special Constraints & Notes
- All enums are stored as int in DB, mapped to C# enums in code.
- Composite PKs: VehicleCoOwner (CoOwnerId, VehicleId), FundUsageVote (FundUsageId, UserId), VehicleUpgradeVote (ProposalId, UserId)
- All CreatedAt/UpdatedAt are UTC.
- All FKs are ON DELETE RESTRICT unless otherwise specified.
- All navigation properties are omitted from DB, only for ORM.

---

## 5. ERD & SQL Schema
- See: ERD_Main.png (diagram)
- See: master_schema.sql (full CREATE TABLE, FK, INDEX, CONSTRAINT statements)

---

## 6. API-DB Mapping
- See: API_DB_MAPPING.md (detailed mapping of API endpoints to DB tables/fields)

---

*This document is auto-generated and should be kept in sync with the codebase and migrations.*
