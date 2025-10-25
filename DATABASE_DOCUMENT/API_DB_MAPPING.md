# API to Database Mapping

This document maps each major API module and endpoint to the underlying database tables and key fields they interact with. Use this as a quick reference to understand the data flow from API to DB.

---

| API Module / Endpoint                | DB Table(s) Involved                | Key Fields / Notes                                  |
|--------------------------------------|-------------------------------------|-----------------------------------------------------|
| Auth (Login/Register/Token)          | User, UserRefreshToken              | Email, PasswordHash, RefreshToken                   |
| User Profile (GET/PUT)               | User                                | Id, Email, FirstName, LastName, etc.                |
| CoOwner (GET/POST/PUT/DELETE)        | CoOwner, User, DrivingLicense       | UserId, LicenseNumber                               |
| Vehicle (CRUD, List, Details)        | Vehicle, VehicleCoOwner, Fund       | Id, Name, OwnershipPercentage, FundId               |
| Vehicle Ownership (Add/Remove)       | VehicleCoOwner, OwnershipHistory    | CoOwnerId, VehicleId, PercentageChange              |
| Booking (CRUD, List, Approve)        | Booking, CheckIn, CheckOut          | Id, CoOwnerId, VehicleId, ApprovedBy                |
| Booking Reminders                    | UserReminderPreference, BookingReminderLog | UserId, BookingId, SentAt                    |
| Maintenance (CRUD, List)             | MaintenanceCost, FundUsage          | Id, VehicleId, Cost, FundUsageId                    |
| Fund (View, Add, Use)                | Fund, FundAddition, FundUsage, FundUsageVote | Id, Amount, UsageTypeEnum, IsAgree         |
| Payment (Create, Status)             | Payment, FundAddition               | Id, UserId, FundAdditionId, StatusEnum              |
| Notification (Send, List, Read)      | NotificationEntity, UserNotification| Id, NotificationType, UserId, ReadAt                |
| Contract (Propose, Vote, Approve)    | VehicleUpgradeProposal, VehicleUpgradeVote | Id, ProposalId, UserId, IsAgree             |
| Ownership Change (Propose, Approve)  | OwnershipChangeRequest, OwnershipHistory | Id, VehicleId, StatusEnum, PercentageChange   |
| Usage Analytics                      | VehicleUsageRecord                  | Id, BookingId, VehicleId, StartTime, EndTime        |
| File Upload                          | FileUpload                          | Id, FileName, UploadedAt                            |
| Configuration                        | Configuration                       | Key, Value                                          |

---

## Example Data Flow
- **Booking a vehicle:**
  1. User (CoOwner) creates a Booking → `Booking` (insert)
  2. System creates CheckIn/CheckOut records → `CheckIn`, `CheckOut` (insert)
  3. If maintenance occurs, `MaintenanceCost` and `FundUsage` may be created
  4. Fund usage may trigger voting → `FundUsageVote`

- **Adding a new vehicle:**
  1. Admin creates Vehicle → `Vehicle` (insert)
  2. Assigns co-owners → `VehicleCoOwner` (insert)
  3. Links to fund → `Fund` (update)

- **Fund addition/payment:**
  1. CoOwner requests fund addition → `FundAddition` (insert)
  2. Payment processed → `Payment` (insert, update status)

---

*For full details, see the API documentation and DATABASE_MASTER_DESIGN.md.*
