-- EV Co-Ownership System
-- master_schema.sql: Full DDL for all tables, keys, constraints, and relationships
-- Compatible with PostgreSQL
CREATE TABLE "User" (
    Id SERIAL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    NormalizedEmail VARCHAR(255),
    PasswordHash VARCHAR(255) NOT NULL,
    PasswordSalt VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    Phone VARCHAR(20),
    DateOfBirth DATE,
    Address VARCHAR(255),
    ProfileImageUrl VARCHAR(255),
    RoleEnum INT NOT NULL,
    StatusEnum INT NOT NULL,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "CoOwner" (
    UserId INT PRIMARY KEY REFERENCES "User"(Id) ON DELETE RESTRICT,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "Fund" (
    Id SERIAL PRIMARY KEY,
    CurrentBalance DECIMAL,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "VehicleStation" (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    Address VARCHAR(255),
    ContactNumber VARCHAR(50),
    LocationLatitude DECIMAL,
    LocationLongitude DECIMAL,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
-- Vehicle must be before VehicleCondition and all referencing tables
CREATE TABLE "Vehicle" (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    Brand VARCHAR(100),
    Model VARCHAR(100),
    Year INT,
    Vin VARCHAR(100) UNIQUE,
    LicensePlate VARCHAR(50) UNIQUE,
    Color VARCHAR(50),
    BatteryCapacity DECIMAL,
    RangeKm INT,
    PurchaseDate DATE,
    PurchasePrice DECIMAL,
    WarrantyUntil DATE,
    DistanceTravelled INT,
    StatusEnum INT,
    VerificationStatusEnum INT,
    LocationLatitude DECIMAL,
    LocationLongitude DECIMAL,
    CreatedBy INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    FundId INT REFERENCES "Fund"(Id) ON DELETE RESTRICT,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
-- VehicleCondition must be after Vehicle
CREATE TABLE "VehicleCondition" (
    Id SERIAL PRIMARY KEY,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    ReportedBy INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    ConditionTypeEnum INT,
    Description TEXT,
    PhotoUrls TEXT,
    OdometerReading INT,
    FuelLevel DECIMAL,
    DamageReported BOOLEAN,
    CreatedAt TIMESTAMP
);
CREATE TABLE "VehicleCoOwner" (
    CoOwnerId INT REFERENCES "CoOwner"(UserId) ON DELETE RESTRICT,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    OwnershipPercentage DECIMAL NOT NULL,
    InvestmentAmount DECIMAL NOT NULL,
    StatusEnum INT,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP,
    PRIMARY KEY (CoOwnerId, VehicleId)
);
CREATE TABLE "Booking" (
    Id SERIAL PRIMARY KEY,
    CoOwnerId INT REFERENCES "CoOwner"(UserId) ON DELETE RESTRICT,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NOT NULL,
    Purpose TEXT,
    StatusEnum INT,
    ApprovedBy INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    TotalCost DECIMAL,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "CheckIn" (
    Id SERIAL PRIMARY KEY,
    BookingId INT REFERENCES "Booking"(Id) ON DELETE RESTRICT,
    StaffId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    VehicleStationId INT REFERENCES "VehicleStation"(Id) ON DELETE RESTRICT,
    VehicleConditionId INT REFERENCES "VehicleCondition"(Id) ON DELETE RESTRICT,
    CheckTime TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "CheckOut" (
    Id SERIAL PRIMARY KEY,
    BookingId INT REFERENCES "Booking"(Id) ON DELETE RESTRICT,
    StaffId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    VehicleStationId INT REFERENCES "VehicleStation"(Id) ON DELETE RESTRICT,
    VehicleConditionId INT REFERENCES "VehicleCondition"(Id) ON DELETE RESTRICT,
    CheckTime TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "FundAddition" (
    Id SERIAL PRIMARY KEY,
    FundId INT REFERENCES "Fund"(Id) ON DELETE RESTRICT,
    CoOwnerId INT REFERENCES "CoOwner"(UserId) ON DELETE RESTRICT,
    Amount DECIMAL NOT NULL,
    PaymentMethodEnum INT,
    TransactionId VARCHAR(100),
    Description TEXT,
    StatusEnum INT,
    CreatedAt TIMESTAMP
);
CREATE TABLE "MaintenanceCost" (
    Id SERIAL PRIMARY KEY,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    BookingId INT REFERENCES "Booking"(Id) ON DELETE RESTRICT,
    MaintenanceTypeEnum INT,
    Description TEXT,
    Cost DECIMAL NOT NULL,
    IsPaid BOOLEAN,
    ServiceProvider VARCHAR(100),
    ServiceDate DATE,
    OdometerReading INT,
    ImageUrl VARCHAR(255),
    CreatedAt TIMESTAMP
);
CREATE TABLE "FundUsage" (
    Id SERIAL PRIMARY KEY,
    FundId INT REFERENCES "Fund"(Id) ON DELETE RESTRICT,
    UsageTypeEnum INT,
    Amount DECIMAL NOT NULL,
    Description TEXT,
    ImageUrl VARCHAR(255),
    MaintenanceCostId INT REFERENCES "MaintenanceCost"(Id) ON DELETE RESTRICT,
    CreatedAt TIMESTAMP
);
CREATE TABLE "FundUsageVote" (
    FundUsageId INT REFERENCES "FundUsage"(Id) ON DELETE RESTRICT,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    IsAgree BOOLEAN NOT NULL,
    CreatedAt TIMESTAMP,
    PRIMARY KEY (FundUsageId, UserId)
);
CREATE TABLE "Payment" (
    Id SERIAL PRIMARY KEY,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    Amount DECIMAL NOT NULL,
    TransactionId VARCHAR(100),
    PaymentGateway VARCHAR(100),
    StatusEnum INT,
    PaidAt TIMESTAMP,
    CreatedAt TIMESTAMP,
    FundAdditionId INT REFERENCES "FundAddition"(Id) ON DELETE RESTRICT
);
CREATE TABLE "NotificationEntity" (
    Id SERIAL PRIMARY KEY,
    NotificationType VARCHAR(100),
    AdditionalDataJson TEXT,
    CreatedAt TIMESTAMP
);
CREATE TABLE "UserNotification" (
    Id SERIAL PRIMARY KEY,
    NotificationId INT REFERENCES "NotificationEntity"(Id) ON DELETE RESTRICT,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    ReadAt TIMESTAMP
);
CREATE TABLE "UserRefreshToken" (
    UserId INT PRIMARY KEY REFERENCES "User"(Id) ON DELETE RESTRICT,
    RefreshToken VARCHAR(255) NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL
);
CREATE TABLE "VehicleVerificationHistory" (
    Id SERIAL PRIMARY KEY,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    StaffId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    StatusEnum INT,
    Notes TEXT,
    ImagesJson TEXT,
    CreatedAt TIMESTAMP
);
CREATE TABLE "VehicleUsageRecord" (
    Id SERIAL PRIMARY KEY,
    BookingId INT REFERENCES "Booking"(Id) ON DELETE RESTRICT,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    CoOwnerId INT REFERENCES "CoOwner"(UserId) ON DELETE RESTRICT,
    CheckInId INT REFERENCES "CheckIn"(Id) ON DELETE RESTRICT,
    CheckOutId INT REFERENCES "CheckOut"(Id) ON DELETE RESTRICT,
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NOT NULL,
    DurationHours DECIMAL,
    DistanceKm INT -- Add other trip/battery/usage fields as needed
);
CREATE TABLE "VehicleUpgradeProposal" (
    Id SERIAL PRIMARY KEY,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    UpgradeType INT,
    Title VARCHAR(255),
    Description TEXT,
    EstimatedCost DECIMAL,
    Justification TEXT,
    ImageUrl VARCHAR(255),
    VendorName VARCHAR(100),
    VendorContact VARCHAR(100),
    ProposedInstallationDate TIMESTAMP,
    EstimatedDurationDays INT,
    ProposedByUserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    ProposedAt TIMESTAMP,
    Status VARCHAR(50),
    ApprovedAt TIMESTAMP,
    RejectedAt TIMESTAMP,
    CancelledAt TIMESTAMP,
    IsExecuted BOOLEAN,
    ExecutedAt TIMESTAMP,
    ActualCost DECIMAL,
    ExecutionNotes TEXT,
    FundUsageId INT REFERENCES "FundUsage"(Id) ON DELETE RESTRICT,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "VehicleUpgradeVote" (
    ProposalId INT REFERENCES "VehicleUpgradeProposal"(Id) ON DELETE RESTRICT,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    IsAgree BOOLEAN NOT NULL,
    Comments TEXT,
    VotedAt TIMESTAMP,
    PRIMARY KEY (ProposalId, UserId)
);
CREATE TABLE "OwnershipChangeRequest" (
    Id SERIAL PRIMARY KEY,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    ProposedByUserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    Reason TEXT,
    StatusEnum INT,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP,
    FinalizedAt TIMESTAMP,
    RequiredApprovals INT,
    CurrentApprovals INT
);
CREATE TABLE "OwnershipHistory" (
    Id SERIAL PRIMARY KEY,
    VehicleId INT REFERENCES "Vehicle"(Id) ON DELETE RESTRICT,
    CoOwnerId INT REFERENCES "CoOwner"(UserId) ON DELETE RESTRICT,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    OwnershipChangeRequestId INT REFERENCES "OwnershipChangeRequest"(Id) ON DELETE RESTRICT,
    PreviousPercentage DECIMAL,
    NewPercentage DECIMAL,
    PercentageChange DECIMAL,
    PreviousInvestment DECIMAL,
    NewInvestment DECIMAL,
    InvestmentChange DECIMAL,
    ChangedAt TIMESTAMP
);
CREATE TABLE "Configuration" (
    Key VARCHAR(100) PRIMARY KEY,
    Value TEXT,
    Description TEXT,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "UserReminderPreference" (
    Id SERIAL PRIMARY KEY,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    HoursBeforeBooking INT,
    Enabled BOOLEAN,
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
CREATE TABLE "BookingReminderLog" (
    Id SERIAL PRIMARY KEY,
    BookingId INT REFERENCES "Booking"(Id) ON DELETE RESTRICT,
    UserId INT REFERENCES "User"(Id) ON DELETE RESTRICT,
    SentAt TIMESTAMP,
    BookingStartTime TIMESTAMP,
    HoursBeforeBooking DOUBLE PRECISION,
    Success BOOLEAN,
    ErrorMessage TEXT
);
CREATE TABLE "FileUpload" (
    Id SERIAL PRIMARY KEY,
    Data BYTEA,
    FileName VARCHAR(255),
    MimeType VARCHAR(100),
    UploadedAt TIMESTAMP
);
CREATE TABLE "DrivingLicense" (
    Id SERIAL PRIMARY KEY,
    CoOwnerId INT REFERENCES "CoOwner"(UserId) ON DELETE RESTRICT,
    LicenseNumber VARCHAR(100),
    IssuedBy VARCHAR(100),
    IssueDate DATE,
    ExpiryDate DATE,
    LicenseImageUrl VARCHAR(255),
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);
-- Indexes, constraints, and additional FKs can be added as needed for performance and integrity.