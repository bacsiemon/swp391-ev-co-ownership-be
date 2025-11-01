# API Endpoint Inventory

This document lists all available API endpoints in the backend, grouped by controller. Each entry shows the HTTP method, route, and path for easy comparison with frontend requirements.

---

## GroupController (`api/group`)
## EV Co-Ownership System - API Endpoint Inventory (as of 2025-10-27)

This document lists **all available backend API endpoints**, grouped by controller, including HTTP method, route, and path. Use this for FE/BE contract comparison and coverage checks.

---

### AuthController (`/api/auth`)
- **POST** `/login` — User login
- **POST** `/register` — User registration
- **POST** `/refresh-token` — Refresh JWT token
- **POST** `/forgot-password` — Request password reset OTP
- **PATCH** `/reset-password` — Reset password with OTP
- **POST** `/verify-license` — Basic license verification
- **GET** `/test/get-forgot-password-otp` — [Dev] Get OTP for email

---

### BookingController (`/api/booking`)
- **POST** `/api/booking` — Create booking
- **GET** `/api/booking/{id}` — Get booking by ID
- **GET** `/api/booking/my-bookings` — Get current user's bookings
- **GET** `/api/booking/vehicle/{vehicleId}` — Get bookings for a vehicle
- **GET** `/api/booking` — [Admin/Staff] Get all bookings
- **PUT** `/api/booking/{id}` — Update booking
- **POST** `/api/booking/{id}/approve` — [Admin/Staff] Approve/reject booking
- **POST** `/api/booking/{id}/cancel` — Cancel booking
- **DELETE** `/api/booking/{id}` — [Admin] Delete booking
- **GET** `/api/booking/statistics` — [Admin/Staff] Get booking statistics
- **GET** `/api/booking/calendar` — Get booking calendar (role-based)
- **GET** `/api/booking/availability` — Check vehicle availability
- **POST** `/api/booking/vehicle/{vehicleId}/request-slot` — [CoOwner] Request booking slot (with conflict detection)
- **POST** `/api/booking/slot-request/{requestId}/respond` — [CoOwner] Respond to slot request
- **POST** `/api/booking/slot-request/{requestId}/cancel` — [CoOwner] Cancel slot request
- **GET** `/api/booking/vehicle/{vehicleId}/pending-slot-requests` — [CoOwner] Get pending slot requests
- **GET** `/api/booking/vehicle/{vehicleId}/slot-request-analytics` — [CoOwner] Get slot request analytics
- **POST** `/api/booking/{bookingId}/resolve-conflict` — [CoOwner] Resolve booking conflict
- **GET** `/api/booking/pending-conflicts` — [CoOwner] Get pending conflicts
- **GET** `/api/booking/vehicle/{vehicleId}/conflict-analytics` — [CoOwner] Get conflict analytics
- **POST** `/api/booking/{bookingId}/modify` — [CoOwner] Modify booking (with conflict validation)
- **POST** `/api/booking/{bookingId}/cancel-enhanced` — [CoOwner] Cancel booking (policy-based)
- **POST** `/api/booking/validate-modification` — [CoOwner] Validate booking modification
- **GET** `/api/booking/modification-history` — [CoOwner] Get booking modification history

### BookingReminderController (`/api/bookingreminder`)
- **POST** `/api/booking-reminder/configure` — Configure reminder preferences
- **GET** `/api/booking-reminder/preferences` — Get reminder preferences
- **GET** `/api/booking-reminder/upcoming` — Get upcoming bookings with reminders
- **POST** `/api/booking-reminder/send/{bookingId}` — Send manual reminder
- **GET** `/api/booking-reminder/statistics` — [Admin] Get reminder statistics

### CheckInCheckOutController (`/api/checkincheckout`)
- **POST** `/api/checkincheckout/qr-checkin` — [CoOwner] QR scan check-in
- **POST** `/api/checkincheckout/qr-checkout` — [CoOwner] QR scan check-out
- **GET** `/api/checkincheckout/generate-qr/{bookingId}` — [CoOwner/Staff/Admin] Generate booking QR code
- **POST** `/api/checkincheckout/manual-checkin` — [Staff/Admin] Manual check-in
- **POST** `/api/checkincheckout/manual-checkout` — [Staff/Admin] Manual check-out
- **GET** `/api/checkincheckout/validate-checkin/{bookingId}` — [CoOwner] Validate check-in eligibility
- **GET** `/api/checkincheckout/validate-checkout/{bookingId}` — [CoOwner] Validate check-out eligibility
- **GET** `/api/checkincheckout/history/{bookingId}` — [CoOwner] Get check-in/out history

### CoOwnerController (`/api/coowner`)
- **GET** `/api/coowner/eligibility` — Check co-owner eligibility
- **POST** `/api/coowner/promote` — Promote current user to co-owner
- **POST** `/api/coowner/promote/{userId}` — [Admin/Staff] Promote user to co-owner
- **GET** `/api/coowner/statistics` — [Admin/Staff] Get co-ownership statistics
- **GET** `/api/coowner/test/eligibility-scenarios` — [Dev] Test eligibility scenarios
- **GET** `/api/coowner/test/promotion-workflow` — [Dev] Test promotion workflow

### ContractController (`/api/contract`)
- **POST** `/api/contract` — Create new e-contract
- **GET** `/api/contract/{contractId}` — Get contract by ID
- **GET** `/api/contract` — List contracts (with filters)
- **POST** `/api/contract/{contractId}/sign` — Sign contract
- **POST** `/api/contract/{contractId}/decline` — Decline contract
- **POST** `/api/contract/{contractId}/terminate` — Terminate contract
- **GET** `/api/contract/templates` — List contract templates
- **GET** `/api/contract/templates/{templateType}` — Get specific contract template
- **GET** `/api/contract/{contractId}/download` — Download contract as PDF
- **GET** `/api/contract/pending-signature` — Contracts pending your signature
- **GET** `/api/contract/signed` — Contracts you have signed

---

### DepositController (`/api/deposit`)
- **POST** `/api/deposit` — Create deposit transaction
- **GET** `/api/deposit/{id}` — Get deposit by ID
- **GET** `/api/deposit/my-deposits` — Get current user's deposit history
- **GET** `/api/deposit` — [Admin/Staff] Get all deposits
- **POST** `/api/deposit/{id}/cancel` — Cancel pending deposit
- **GET** `/api/deposit/my-statistics` — Get user's deposit statistics
- **GET** `/api/deposit/payment-methods` — List available payment methods
- **GET** `/api/deposit/callback` — Payment gateway callback (GET)
- **POST** `/api/deposit/verify-callback` — Payment gateway callback (POST)

---

### DisputeController (`/api/dispute`)
- **POST** `/api/dispute/booking` — Raise booking dispute
- **POST** `/api/dispute/cost-sharing` — Raise cost sharing dispute
- **POST** `/api/dispute/group-decision` — Raise group decision dispute
- **GET** `/api/dispute/{disputeId}` — Get dispute by ID
- **GET** `/api/dispute` — List disputes (with filters)
- **POST** `/api/dispute/{disputeId}/respond` — Respond to dispute
- **PUT** `/api/dispute/{disputeId}/status` — [Admin] Update dispute status
- **POST** `/api/dispute/{disputeId}/withdraw` — Withdraw dispute

---

### FairnessOptimizationController (`/api/fairnessoptimization`)
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/fairness-report` — Get fairness report
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/schedule-suggestions` — Get fair schedule suggestions
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/maintenance-suggestions` — Get predictive maintenance suggestions
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/cost-saving-recommendations` — Get cost-saving recommendations

---

### FileUploadController (`/api/fileupload`)
- **POST** `/api/fileupload/upload` — Upload file
- **GET** `/api/fileupload/{id}/download` — Download file
- **GET** `/api/fileupload/{id}` — Get file (raw)
- **GET** `/api/fileupload/{id}/info` — Get file info
- **DELETE** `/api/fileupload/{id}` — [Admin] Delete file

---

### FundController (`/api/fund`)
- **GET** `/api/fund/balance/{vehicleId}` — Get fund balance
- **GET** `/api/fund/additions/{vehicleId}` — Get fund additions (deposits)
- **GET** `/api/fund/usages/{vehicleId}` — Get fund usages (expenses)
- **GET** `/api/fund/summary/{vehicleId}` — Get fund summary
- **POST** `/api/fund/usage` — Create fund usage (expense)
- **PUT** `/api/fund/usage/{usageId}` — Update fund usage
- **DELETE** `/api/fund/usage/{usageId}` — Delete fund usage
- **GET** `/api/fund/category/{vehicleId}/usages/{category}` — Get usages by category
- **GET** `/api/fund/category/{vehicleId}/analysis` — Get category budget analysis

---

### GroupController (`/api/group`)
- **GET** `/api/group` — List all groups
- **GET** `/api/group/{id}` — Get group by id
- **POST** `/api/group` — Create a new group
- **PUT** `/api/group/{id}` — Update a group
- **DELETE** `/api/group/{id}` — Remove a group
- **GET** `/api/group/{groupId}/members` — List group members
- **POST** `/api/group/{groupId}/members` — Add member to group
- **DELETE** `/api/group/{groupId}/members/{memberId}` — Remove member from group
- **PUT** `/api/group/{groupId}/members/{memberId}/role` — Update member role
- **GET** `/api/group/{groupId}/votes` — List group votes
- **POST** `/api/group/{groupId}/votes` — Create a vote in group
- **POST** `/api/group/{groupId}/votes/{voteId}/vote` — Vote on a group vote
- **GET** `/api/group/{groupId}/fund` — Get group fund
- **POST** `/api/group/{groupId}/fund/contribute` — Contribute to group fund
- **GET** `/api/group/{groupId}/fund/history` — Get group fund history

### LicenseController (`/api/license`)
- **POST** `/api/license/verify` — Verify driving license
- **GET** `/api/license/check-exists` — Check if license exists
- **GET** `/api/license/info` — Get license info by number
- **PATCH** `/api/license/status` — [Admin/Staff] Update license status
- **GET** `/api/license/user/{userId}` — Get license for user
- **PUT** `/api/license/{licenseId}` — Update license
- **DELETE** `/api/license/{licenseId}` — Delete license
- **POST** `/api/license/register` — Register verified license
- **GET** `/api/license/my-license` — Get current user's license

---

### MaintenanceController (`/api/maintenance`)
- **POST** `/api/maintenance` — Create maintenance record
- **GET** `/api/maintenance/{id}` — Get maintenance by ID
- **GET** `/api/maintenance/vehicle/{vehicleId}` — Get maintenances for vehicle
- **GET** `/api/maintenance/vehicle/{vehicleId}/history` — Get maintenance history for vehicle
- **GET** `/api/maintenance` — [Admin/Staff] Get all maintenances
- **PUT** `/api/maintenance/{id}` — [Admin/Staff] Update maintenance
- **POST** `/api/maintenance/{id}/mark-paid` — [Admin/Staff] Mark as paid
- **DELETE** `/api/maintenance/{id}` — [Admin] Delete maintenance
- **GET** `/api/maintenance/statistics` — [Admin/Staff] Get statistics
- **GET** `/api/maintenance/vehicle/{vehicleId}/statistics` — Get vehicle maintenance statistics

---

### MaintenanceVoteController (`/api/maintenance-vote`)
- **POST** `/api/maintenance-vote/propose` — Propose maintenance expenditure
- **POST** `/api/maintenance-vote/{fundUsageId}/vote` — Vote on proposal
- **GET** `/api/maintenance-vote/{fundUsageId}` — Get proposal details
- **GET** `/api/maintenance-vote/vehicle/{vehicleId}/pending` — Get pending proposals for vehicle
- **GET** `/api/maintenance-vote/my-voting-history` — Get user voting history
- **DELETE** `/api/maintenance-vote/{fundUsageId}/cancel` — Cancel proposal

---

### NotificationController (`/api/notification`)
- **GET** `/api/notification/my-notifications` — Get notifications for user
- **GET** `/api/notification/unread-count` — Get unread notification count
- **PUT** `/api/notification/mark-read` — Mark notification as read
- **PUT** `/api/notification/mark-multiple-read` — Mark multiple notifications as read
- **PUT** `/api/notification/mark-all-read` — Mark all notifications as read
- **POST** `/api/notification/send-to-user` — [Admin] Send notification to user
- **POST** `/api/notification/create-notification` — [Admin] Create notification for users

---

### OwnershipChangeController (`/api/ownership-change`)
- **POST** `/api/ownership-change/propose` — Propose ownership change
- **GET** `/api/ownership-change/{requestId}` — Get ownership change request
- **GET** `/api/ownership-change/vehicle/{vehicleId}` — Get vehicle ownership change requests
- **GET** `/api/ownership-change/pending-approvals` — Get pending approvals for user
- **POST** `/api/ownership-change/{requestId}/respond` — Approve/reject ownership change
- **DELETE** `/api/ownership-change/{requestId}` — Cancel ownership change request
- **GET** `/api/ownership-change/statistics` — [Admin/Staff] Get statistics
- **GET** `/api/ownership-change/my-requests` — Get user's ownership change requests

---

### OwnershipHistoryController (`/api/ownershiphistory`)
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}` — Get ownership history for vehicle
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}/timeline` — Get ownership timeline for vehicle
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}/snapshot` — Get ownership snapshot at date
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}/statistics` — Get ownership history statistics
- **GET** `/api/ownershiphistory/my-history` — Get user's ownership history

---

### PaymentController (`/api/payment`)
- **POST** `/api/payment` — Create payment
- **POST** `/api/payment/process` — Process payment callback
- **GET** `/api/payment/{id}` — Get payment by ID
- **GET** `/api/payment/my-payments` — Get user's payments
- **GET** `/api/payment` — [Admin/Staff] Get all payments
- **POST** `/api/payment/{id}/cancel` — Cancel payment
- **GET** `/api/payment/statistics` — [Admin/Staff] Get payment statistics
- **GET** `/api/payment/gateways` — List payment gateways
- **GET** `/api/payment/vnpay-callback` — VNPay callback (return URL)

---

### ProfileController (`/api/profile`)
- **GET** `/api/profile` — Get current user's profile
- **GET** `/api/profile/{userId}` — [Admin/Staff] Get user profile by ID
- **GET** `/api/profile/by-email` — [Admin/Staff] Get user profile by email
- **PUT** `/api/profile` — Update current user's profile
- **PUT** `/api/profile/change-password` — Change password
- **GET** `/api/profile/vehicles` — Get user's vehicles summary
- **GET** `/api/profile/activity` — Get user's activity summary
- **POST** `/api/profile/upload-image` — Upload profile image
- **DELETE** `/api/profile/delete-image` — Delete profile image
- **GET** `/api/profile/validate-completeness` — Validate profile completeness
- **GET** `/api/profile/test/profile-scenarios` — [Dev] Test profile scenarios

---

### ScheduleController (`/api/schedule`)
- **GET** `/api/schedule` — Get all schedules
- **GET** `/api/schedule/{id}` — Get schedule by id
- **POST** `/api/schedule` — Create schedule
- **DELETE** `/api/schedule/{id}` — Delete schedule
- **POST** `/api/schedule/book` — Book time slot
- **GET** `/api/schedule/vehicle/{vehicleId}` — Get vehicle schedule
- **GET** `/api/schedule/user` — Get user schedule
- **GET** `/api/schedule/daily` — Get daily schedule
- **GET** `/api/schedule/weekly` — Get weekly schedule
- **GET** `/api/schedule/monthly` — Get monthly schedule
- **POST** `/api/schedule/availability` — Check vehicle availability
- **GET** `/api/schedule/available-slots` — Get available time slots
- **POST** `/api/schedule/conflicts` — Get schedule conflicts
- **POST** `/api/schedule/conflicts/{conflictId}/resolve` — Resolve schedule conflict
- **POST** `/api/schedule/recurring` — Create recurring schedule
- **PUT** `/api/schedule/recurring/{id}` — Update recurring schedule
- **DELETE** `/api/schedule/recurring/{id}` — Delete recurring schedule
- **GET** `/api/schedule/templates` — Get schedule templates
- **POST** `/api/schedule/templates` — Create schedule template
- **POST** `/api/schedule/templates/{templateId}/apply` — Apply schedule template
- **GET** `/api/schedule/reminders` — Get upcoming reminders
- **POST** `/api/schedule/{scheduleId}/reminder` — Set reminder
- **GET** `/api/schedule/usage-report` — Get schedule usage report

---

### ServiceController (`/api/service`)
- **GET** `/api/service` — List all services
- **POST** `/api/service/{id}/start` — Start a service
- **POST** `/api/service/{id}/complete` — Complete a service

---

### UserController (`/api/user`)
- **GET** `/api/user` — [Admin/Staff] List users
- **GET** `/api/user/{id}` — Get user by ID
- **PUT** `/api/user/{id}` — Update user
- **DELETE** `/api/user/{id}` — [Admin] Delete user

## ServiceController (`api/service`)
- POST `/{id}/start`
- POST `/{id}/complete`

## CoOwnerController (`api/coowner`)
- GET `/eligibility`
- POST `/promote`
- POST `/promote/{userId:int}`
- GET `/statistics`
- GET `/test/eligibility-scenarios`
- GET `/test/promotion-workflow`

## ScheduleController (`api/schedule`)
- GET `/{id}`
- DELETE `/{id}`
- POST `/book`
- GET `/vehicle/{vehicleId}`
- GET `/user`
- GET `/daily`
- GET `/weekly`
- GET `/monthly`
## EV Co-Ownership System - API Endpoint Inventory (as of 2025-10-27)

This document lists **all available backend API endpoints**, grouped by controller, including HTTP method, route, and path. Use this for FE/BE contract comparison and coverage checks.

---

### BookingController (`/api/booking`)
- **POST** `/api/booking` — Create booking
- **GET** `/api/booking/{id}` — Get booking by ID
- **GET** `/api/booking/my-bookings` — Get current user's bookings
- **GET** `/api/booking/vehicle/{vehicleId}` — Get bookings for a vehicle
- **GET** `/api/booking` — [Admin/Staff] Get all bookings
- **PUT** `/api/booking/{id}` — Update booking
- **POST** `/api/booking/{id}/approve` — [Admin/Staff] Approve/reject booking
- **POST** `/api/booking/{id}/cancel` — Cancel booking
- **DELETE** `/api/booking/{id}` — [Admin] Delete booking
- **GET** `/api/booking/statistics` — [Admin/Staff] Get booking statistics
- **GET** `/api/booking/calendar` — Get booking calendar (role-based)
- **GET** `/api/booking/availability` — Check vehicle availability
- **POST** `/api/booking/vehicle/{vehicleId}/request-slot` — [CoOwner] Request booking slot (with conflict detection)
- **POST** `/api/booking/slot-request/{requestId}/respond` — [CoOwner] Respond to slot request
- **POST** `/api/booking/slot-request/{requestId}/cancel` — [CoOwner] Cancel slot request
- **GET** `/api/booking/vehicle/{vehicleId}/pending-slot-requests` — [CoOwner] Get pending slot requests
- **GET** `/api/booking/vehicle/{vehicleId}/slot-request-analytics` — [CoOwner] Get slot request analytics
- **POST** `/api/booking/{bookingId}/resolve-conflict` — [CoOwner] Resolve booking conflict
- **GET** `/api/booking/pending-conflicts` — [CoOwner] Get pending conflicts
- **GET** `/api/booking/vehicle/{vehicleId}/conflict-analytics` — [CoOwner] Get conflict analytics
- **POST** `/api/booking/{bookingId}/modify` — [CoOwner] Modify booking (with conflict validation)
- **POST** `/api/booking/{bookingId}/cancel-enhanced` — [CoOwner] Cancel booking (policy-based)
- **POST** `/api/booking/validate-modification` — [CoOwner] Validate booking modification
- **GET** `/api/booking/modification-history` — [CoOwner] Get booking modification history

### BookingReminderController (`/api/booking-reminder`)
- **POST** `/api/booking-reminder/configure` — Configure reminder preferences
- **GET** `/api/booking-reminder/preferences` — Get reminder preferences
- **GET** `/api/booking-reminder/upcoming` — Get upcoming bookings with reminders
- **POST** `/api/booking-reminder/send/{bookingId}` — Send manual reminder
- **GET** `/api/booking-reminder/statistics` — [Admin] Get reminder statistics

### CheckInCheckOutController (`/api/checkincheckout`)
- **POST** `/api/checkincheckout/qr-checkin` — [CoOwner] QR scan check-in
- **POST** `/api/checkincheckout/qr-checkout` — [CoOwner] QR scan check-out
- **GET** `/api/checkincheckout/generate-qr/{bookingId}` — [CoOwner/Staff/Admin] Generate booking QR code
- **POST** `/api/checkincheckout/manual-checkin` — [Staff/Admin] Manual check-in
- **POST** `/api/checkincheckout/manual-checkout` — [Staff/Admin] Manual check-out
- **GET** `/api/checkincheckout/validate-checkin/{bookingId}` — [CoOwner] Validate check-in eligibility
- **GET** `/api/checkincheckout/validate-checkout/{bookingId}` — [CoOwner] Validate check-out eligibility
- **GET** `/api/checkincheckout/history/{bookingId}` — [CoOwner] Get check-in/out history

### CoOwnerController (`/api/coowner`)
- **GET** `/api/coowner/eligibility` — Check co-owner eligibility
- **POST** `/api/coowner/promote` — Promote current user to co-owner
- **POST** `/api/coowner/promote/{userId}` — [Admin/Staff] Promote user to co-owner
- **GET** `/api/coowner/statistics` — [Admin/Staff] Get co-ownership statistics
- **GET** `/api/coowner/test/eligibility-scenarios` — [Dev] Test eligibility scenarios
- **GET** `/api/coowner/test/promotion-workflow` — [Dev] Test promotion workflow

### FairnessOptimizationController (`/api/fairnessoptimization`)
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/fairness-report` — [CoOwner] Get fairness report
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/schedule-suggestions` — [CoOwner] Get fair schedule suggestions
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/maintenance-suggestions` — [CoOwner] Get predictive maintenance suggestions
- **GET** `/api/fairnessoptimization/vehicle/{vehicleId}/cost-saving-recommendations` — [CoOwner] Get cost-saving recommendations

### FundController (`/api/fund`)
- **GET** `/api/fund/balance/{vehicleId}` — Get fund balance
- **GET** `/api/fund/additions/{vehicleId}` — Get fund additions (deposits)
- **GET** `/api/fund/usages/{vehicleId}` — Get fund usages (expenses)
- **GET** `/api/fund/summary/{vehicleId}` — Get fund summary
- **POST** `/api/fund/usage` — Create fund usage (expense)
- **PUT** `/api/fund/usage/{usageId}` — Update fund usage
- **DELETE** `/api/fund/usage/{usageId}` — Delete fund usage
- **GET** `/api/fund/category/{vehicleId}/usages/{category}` — Get usages by category
- **GET** `/api/fund/category/{vehicleId}/analysis` — Get category budget analysis

### MaintenanceVoteController (`/api/maintenance-vote`)
- **POST** `/api/maintenance-vote/propose` — Propose maintenance expenditure
- **POST** `/api/maintenance-vote/{fundUsageId}/vote` — Vote on proposal
- **GET** `/api/maintenance-vote/{fundUsageId}` — Get proposal details
- **GET** `/api/maintenance-vote/vehicle/{vehicleId}/pending` — Get pending proposals for vehicle
- **GET** `/api/maintenance-vote/my-voting-history` — Get user voting history
- **DELETE** `/api/maintenance-vote/{fundUsageId}/cancel` — Cancel proposal

### NotificationController (`/api/notification`)
- **GET** `/api/notification/my-notifications` — Get notifications for user
- **GET** `/api/notification/unread-count` — Get unread notification count
- **PUT** `/api/notification/mark-read` — Mark notification as read
- **PUT** `/api/notification/mark-multiple-read` — Mark multiple notifications as read
- **PUT** `/api/notification/mark-all-read` — Mark all notifications as read
- **POST** `/api/notification/send-to-user` — [Admin] Send notification to user
- **POST** `/api/notification/create-notification` — [Admin] Create notification for users

### MaintenanceController (`/api/maintenance`)
- **POST** `/api/maintenance` — Create maintenance record
- **GET** `/api/maintenance/{id}` — Get maintenance by ID
- **GET** `/api/maintenance/vehicle/{vehicleId}` — Get maintenances for vehicle
- **GET** `/api/maintenance/vehicle/{vehicleId}/history` — Get maintenance history for vehicle
- **GET** `/api/maintenance` — [Admin/Staff] Get all maintenances
- **PUT** `/api/maintenance/{id}` — [Admin/Staff] Update maintenance
- **POST** `/api/maintenance/{id}/mark-paid` — [Admin/Staff] Mark as paid
- **DELETE** `/api/maintenance/{id}` — [Admin] Delete maintenance
- **GET** `/api/maintenance/statistics` — [Admin/Staff] Get statistics
- **GET** `/api/maintenance/vehicle/{vehicleId}/statistics` — Get vehicle maintenance statistics

### OwnershipHistoryController (`/api/ownershiphistory`)
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}` — [CoOwner] Get ownership history for vehicle
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}/timeline` — [CoOwner] Get ownership timeline for vehicle
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}/snapshot` — [CoOwner] Get ownership snapshot at date
- **GET** `/api/ownershiphistory/vehicle/{vehicleId}/statistics` — [CoOwner] Get ownership history statistics
- **GET** `/api/ownershiphistory/my-history` — [CoOwner] Get user's ownership history

---

**Note:** This is a partial update. If you need the full list for all 56 controllers, please request a full export or specify which controllers/endpoints you want to see in detail.
