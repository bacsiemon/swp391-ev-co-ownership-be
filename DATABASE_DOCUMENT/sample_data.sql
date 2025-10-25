-- master_sample_data_fixed.sql
-- Ready-to-run sample data for PostgreSQL that matches master_schema.sql
-- Resets data, restarts identities
TRUNCATE TABLE "VehicleUpgradeVote",
"VehicleUpgradeProposal",
"VehicleStation",
"VehicleVerificationHistory",
"UserRefreshToken",
"UserNotification",
"NotificationEntity",
"Payment",
"FundUsageVote",
"FundUsage",
"FundAddition",
"MaintenanceCost",
"VehicleUsageRecord",
"Booking",
"VehicleCoOwner",
"Vehicle",
"VehicleCondition",
"CheckOut",
"CheckIn",
"CoOwner",
"Fund",
"User" RESTART IDENTITY CASCADE;
-- ---------------------------
-- Users (10)
-- ---------------------------
INSERT INTO "User" (
        Email,
        PasswordHash,
        PasswordSalt,
        Phone,
        RoleEnum,
        StatusEnum,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        'user1@evco.com',
        'hash1',
        'salt1',
        '0909000001',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user2@evco.com',
        'hash2',
        'salt2',
        '0909000002',
        2,
        1,
        NOW(),
        NOW()
    ),
    (
        'user3@evco.com',
        'hash3',
        'salt3',
        '0909000003',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user4@evco.com',
        'hash4',
        'salt4',
        '0909000004',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user5@evco.com',
        'hash5',
        'salt5',
        '0909000005',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user6@evco.com',
        'hash6',
        'salt6',
        '0909000006',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user7@evco.com',
        'hash7',
        'salt7',
        '0909000007',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user8@evco.com',
        'hash8',
        'salt8',
        '0909000008',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user9@evco.com',
        'hash9',
        'salt9',
        '0909000009',
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'user10@evco.com',
        'hash10',
        'salt10',
        '0909000010',
        1,
        1,
        NOW(),
        NOW()
    );
-- ---------------------------
-- Funds (10)
-- ---------------------------
INSERT INTO "Fund" (CurrentBalance, CreatedAt, UpdatedAt)
VALUES (10000000, NOW(), NOW()),
    (5000000, NOW(), NOW()),
    (2000000, NOW(), NOW()),
    (15000000, NOW(), NOW()),
    (8000000, NOW(), NOW()),
    (12000000, NOW(), NOW()),
    (9000000, NOW(), NOW()),
    (3000000, NOW(), NOW()),
    (7000000, NOW(), NOW()),
    (6000000, NOW(), NOW());
-- ---------------------------
-- CoOwners (10)  -- references User
-- ---------------------------
INSERT INTO "CoOwner" (UserId, CreatedAt, UpdatedAt)
VALUES (1, NOW(), NOW()),
    (2, NOW(), NOW()),
    (3, NOW(), NOW()),
    (4, NOW(), NOW()),
    (5, NOW(), NOW()),
    (6, NOW(), NOW()),
    (7, NOW(), NOW()),
    (8, NOW(), NOW()),
    (9, NOW(), NOW()),
    (10, NOW(), NOW());
-- ---------------------------
-- Vehicles (10)  -- references CreatedBy -> User, FundId -> Fund
-- ---------------------------
INSERT INTO "Vehicle" (
        Name,
        Description,
        Brand,
        Model,
        Year,
        Vin,
        LicensePlate,
        Color,
        BatteryCapacity,
        RangeKm,
        PurchaseDate,
        PurchasePrice,
        WarrantyUntil,
        DistanceTravelled,
        StatusEnum,
        VerificationStatusEnum,
        LocationLatitude,
        LocationLongitude,
        CreatedBy,
        FundId,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        'EV Car 1',
        'Electric car for city use',
        'VinFast',
        'VF e34',
        2023,
        'VIN001',
        '51A-00001',
        'White',
        42.0,
        300,
        '2023-01-10',
        700000000,
        '2026-01-10',
        12000,
        1,
        1,
        10.762622,
        106.660172,
        1,
        1,
        NOW(),
        NOW()
    ),
    (
        'EV Bike 1',
        'Electric bike for short trips',
        'Yadea',
        'G5',
        2022,
        'VIN002',
        '59B1-00002',
        'Red',
        2.0,
        60,
        '2022-06-15',
        20000000,
        '2025-06-15',
        3500,
        1,
        1,
        10.762700,
        106.660200,
        3,
        2,
        NOW(),
        NOW()
    ),
    (
        'EV Scooter 1',
        'Electric scooter for daily commute',
        'Pega',
        'Aura',
        2024,
        'VIN003',
        '59C1-00003',
        'Blue',
        1.5,
        80,
        '2024-03-20',
        18000000,
        '2027-03-20',
        500,
        1,
        1,
        10.762800,
        106.660300,
        4,
        3,
        NOW(),
        NOW()
    ),
    (
        'EV Car 2',
        'Family electric car',
        'Tesla',
        'Model 3',
        2023,
        'VIN004',
        '51A-00004',
        'Black',
        60.0,
        400,
        '2023-09-01',
        1200000000,
        '2026-09-01',
        8000,
        1,
        1,
        10.762900,
        106.660400,
        5,
        4,
        NOW(),
        NOW()
    ),
    (
        'EV Bike 2',
        'Electric bike for delivery',
        'VinFast',
        'Klara',
        2021,
        'VIN005',
        '59B2-00005',
        'Green',
        1.8,
        70,
        '2021-12-05',
        17000000,
        '2024-12-05',
        9000,
        1,
        1,
        10.763000,
        106.660500,
        1,
        5,
        NOW(),
        NOW()
    ),
    (
        'EV Car 3',
        'Luxury electric car',
        'BMW',
        'i4',
        2022,
        'VIN006',
        '51A-00006',
        'Silver',
        80.0,
        500,
        '2022-05-10',
        1500000000,
        '2025-05-10',
        5000,
        1,
        1,
        10.763100,
        106.660600,
        6,
        6,
        NOW(),
        NOW()
    ),
    (
        'EV Bike 3',
        'Sport electric bike',
        'Honda',
        'PCX',
        2023,
        'VIN007',
        '59B3-00007',
        'Yellow',
        2.2,
        75,
        '2023-08-15',
        22000000,
        '2026-08-15',
        2000,
        1,
        1,
        10.763200,
        106.660700,
        7,
        7,
        NOW(),
        NOW()
    ),
    (
        'EV Scooter 2',
        'Compact scooter',
        'SYM',
        'Galaxy',
        2024,
        'VIN008',
        '59C2-00008',
        'Pink',
        1.6,
        85,
        '2024-04-20',
        19000000,
        '2027-04-20',
        600,
        1,
        1,
        10.763300,
        106.660800,
        8,
        8,
        NOW(),
        NOW()
    ),
    (
        'EV Car 4',
        'SUV electric',
        'Kia',
        'EV6',
        2023,
        'VIN009',
        '51A-00009',
        'Blue',
        77.4,
        420,
        '2023-11-01',
        1300000000,
        '2026-11-01',
        7000,
        1,
        1,
        10.763400,
        106.660900,
        9,
        9,
        NOW(),
        NOW()
    ),
    (
        'EV Bike 4',
        'Touring bike',
        'Suzuki',
        'Burgman',
        2022,
        'VIN010',
        '59B4-00010',
        'Orange',
        2.5,
        80,
        '2022-10-05',
        25000000,
        '2025-10-05',
        4000,
        1,
        1,
        10.763500,
        106.661000,
        10,
        10,
        NOW(),
        NOW()
    );
-- ---------------------------
-- VehicleCoOwner (links) (10)
-- ---------------------------
INSERT INTO "VehicleCoOwner" (
        CoOwnerId,
        VehicleId,
        OwnershipPercentage,
        InvestmentAmount,
        StatusEnum,
        CreatedAt,
        UpdatedAt
    )
VALUES (1, 1, 50.0, 350000000, 1, NOW(), NOW()),
    (3, 1, 50.0, 350000000, 1, NOW(), NOW()),
    (4, 2, 100.0, 20000000, 1, NOW(), NOW()),
    (5, 3, 100.0, 18000000, 1, NOW(), NOW()),
    (1, 4, 100.0, 1200000000, 1, NOW(), NOW()),
    (6, 5, 100.0, 17000000, 1, NOW(), NOW()),
    (7, 6, 100.0, 1500000000, 1, NOW(), NOW()),
    (8, 7, 100.0, 22000000, 1, NOW(), NOW()),
    (9, 8, 100.0, 19000000, 1, NOW(), NOW()),
    (10, 9, 100.0, 1300000000, 1, NOW(), NOW());
-- ---------------------------
-- Booking (10)  -- references VehicleId, CoOwnerId
-- ---------------------------
INSERT INTO "Booking" (
        VehicleId,
        CoOwnerId,
        StartTime,
        EndTime,
        StatusEnum,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        1,
        1,
        NOW() - INTERVAL '10 days',
        NOW() - INTERVAL '10 days' + INTERVAL '2 hours',
        1,
        NOW() - INTERVAL '10 days',
        NOW() - INTERVAL '10 days'
    ),
    (
        2,
        4,
        NOW() - INTERVAL '9 days',
        NOW() - INTERVAL '9 days' + INTERVAL '1 hour',
        1,
        NOW() - INTERVAL '9 days',
        NOW() - INTERVAL '9 days'
    ),
    (
        3,
        5,
        NOW() - INTERVAL '8 days',
        NOW() - INTERVAL '8 days' + INTERVAL '3 hours',
        1,
        NOW() - INTERVAL '8 days',
        NOW() - INTERVAL '8 days'
    ),
    (
        4,
        1,
        NOW() - INTERVAL '7 days',
        NOW() - INTERVAL '7 days' + INTERVAL '1.5 hours',
        1,
        NOW() - INTERVAL '7 days',
        NOW() - INTERVAL '7 days'
    ),
    (
        5,
        3,
        NOW() - INTERVAL '6 days',
        NOW() - INTERVAL '6 days' + INTERVAL '2.5 hours',
        1,
        NOW() - INTERVAL '6 days',
        NOW() - INTERVAL '6 days'
    ),
    (
        6,
        6,
        NOW() - INTERVAL '5 days',
        NOW() - INTERVAL '5 days' + INTERVAL '2 hours',
        1,
        NOW() - INTERVAL '5 days',
        NOW() - INTERVAL '5 days'
    ),
    (
        7,
        7,
        NOW() - INTERVAL '4 days',
        NOW() - INTERVAL '4 days' + INTERVAL '1 hour',
        1,
        NOW() - INTERVAL '4 days',
        NOW() - INTERVAL '4 days'
    ),
    (
        8,
        8,
        NOW() - INTERVAL '3 days',
        NOW() - INTERVAL '3 days' + INTERVAL '3 hours',
        1,
        NOW() - INTERVAL '3 days',
        NOW() - INTERVAL '3 days'
    ),
    (
        9,
        9,
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '2 days' + INTERVAL '1.5 hours',
        1,
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '2 days'
    ),
    (
        10,
        10,
        NOW() - INTERVAL '1 day',
        NOW() - INTERVAL '1 day' + INTERVAL '2.5 hours',
        1,
        NOW() - INTERVAL '1 day',
        NOW() - INTERVAL '1 day'
    );
-- ---------------------------
-- VehicleStation (10)
-- ---------------------------
INSERT INTO "VehicleStation" (
        Name,
        Description,
        Address,
        ContactNumber,
        LocationLatitude,
        LocationLongitude,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        'Station 1',
        'Main EV station',
        '100 EV Plaza',
        '0909000001',
        10.762622,
        106.660172,
        NOW(),
        NOW()
    ),
    (
        'Station 2',
        'Bike station',
        '200 EV Plaza',
        '0909000002',
        10.762700,
        106.660200,
        NOW(),
        NOW()
    ),
    (
        'Station 3',
        'Scooter station',
        '300 EV Plaza',
        '0909000003',
        10.762800,
        106.660300,
        NOW(),
        NOW()
    ),
    (
        'Station 4',
        'Car station',
        '400 EV Plaza',
        '0909000004',
        10.762900,
        106.660400,
        NOW(),
        NOW()
    ),
    (
        'Station 5',
        'Delivery station',
        '500 EV Plaza',
        '0909000005',
        10.763000,
        106.660500,
        NOW(),
        NOW()
    ),
    (
        'Station 6',
        'EV station 6',
        '600 EV Plaza',
        '0909000006',
        10.763100,
        106.660600,
        NOW(),
        NOW()
    ),
    (
        'Station 7',
        'EV station 7',
        '700 EV Plaza',
        '0909000007',
        10.763200,
        106.660700,
        NOW(),
        NOW()
    ),
    (
        'Station 8',
        'EV station 8',
        '800 EV Plaza',
        '0909000008',
        10.763300,
        106.660800,
        NOW(),
        NOW()
    ),
    (
        'Station 9',
        'EV station 9',
        '900 EV Plaza',
        '0909000009',
        10.763400,
        106.660900,
        NOW(),
        NOW()
    ),
    (
        'Station 10',
        'EV station 10',
        '1000 EV Plaza',
        '0909000010',
        10.763500,
        106.661000,
        NOW(),
        NOW()
    );
-- ---------------------------
-- VehicleCondition (10)
-- ---------------------------
INSERT INTO "VehicleCondition" (
        VehicleId,
        ReportedBy,
        ConditionTypeEnum,
        Description,
        PhotoUrls,
        OdometerReading,
        FuelLevel,
        DamageReported,
        CreatedAt
    )
VALUES (
        1,
        2,
        1,
        'Good condition',
        NULL,
        12000,
        100,
        FALSE,
        NOW()
    ),
    (
        2,
        2,
        1,
        'No issues',
        NULL,
        3500,
        90,
        FALSE,
        NOW()
    ),
    (
        3,
        2,
        2,
        'Minor scratch',
        NULL,
        500,
        80,
        TRUE,
        NOW()
    ),
    (4, 2, 1, 'Clean', NULL, 8000, 95, FALSE, NOW()),
    (5, 2, 1, 'Normal', NULL, 9000, 85, FALSE, NOW()),
    (
        6,
        2,
        1,
        'Battery checked',
        NULL,
        4000,
        98,
        FALSE,
        NOW()
    ),
    (
        7,
        2,
        2,
        'Tire needs air',
        NULL,
        2500,
        70,
        TRUE,
        NOW()
    ),
    (8, 2, 1, 'All OK', NULL, 7500, 92, FALSE, NOW()),
    (
        9,
        2,
        1,
        'Software updated',
        NULL,
        9000,
        88,
        FALSE,
        NOW()
    ),
    (
        10,
        2,
        1,
        'Full inspection',
        NULL,
        10000,
        99,
        FALSE,
        NOW()
    );
-- ---------------------------
-- CheckIn (10)
-- ---------------------------
INSERT INTO "CheckIn" (
        BookingId,
        StaffId,
        VehicleStationId,
        VehicleConditionId,
        CheckTime,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        1,
        2,
        1,
        1,
        NOW() - INTERVAL '10 days',
        NOW() - INTERVAL '10 days',
        NOW() - INTERVAL '10 days'
    ),
    (
        2,
        2,
        2,
        2,
        NOW() - INTERVAL '9 days',
        NOW() - INTERVAL '9 days',
        NOW() - INTERVAL '9 days'
    ),
    (
        3,
        2,
        3,
        3,
        NOW() - INTERVAL '8 days',
        NOW() - INTERVAL '8 days',
        NOW() - INTERVAL '8 days'
    ),
    (
        4,
        2,
        4,
        4,
        NOW() - INTERVAL '7 days',
        NOW() - INTERVAL '7 days',
        NOW() - INTERVAL '7 days'
    ),
    (
        5,
        2,
        5,
        5,
        NOW() - INTERVAL '6 days',
        NOW() - INTERVAL '6 days',
        NOW() - INTERVAL '6 days'
    ),
    (
        6,
        2,
        6,
        6,
        NOW() - INTERVAL '5 days',
        NOW() - INTERVAL '5 days',
        NOW() - INTERVAL '5 days'
    ),
    (
        7,
        2,
        7,
        7,
        NOW() - INTERVAL '4 days',
        NOW() - INTERVAL '4 days',
        NOW() - INTERVAL '4 days'
    ),
    (
        8,
        2,
        8,
        8,
        NOW() - INTERVAL '3 days',
        NOW() - INTERVAL '3 days',
        NOW() - INTERVAL '3 days'
    ),
    (
        9,
        2,
        9,
        9,
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '2 days'
    ),
    (
        10,
        2,
        10,
        10,
        NOW() - INTERVAL '1 days',
        NOW() - INTERVAL '1 days',
        NOW() - INTERVAL '1 days'
    );
-- ---------------------------
-- CheckOut (10)
-- ---------------------------
INSERT INTO "CheckOut" (
        BookingId,
        StaffId,
        VehicleStationId,
        VehicleConditionId,
        CheckTime,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        1,
        2,
        1,
        1,
        NOW() - INTERVAL '10 days' + INTERVAL '2 hours',
        NOW() - INTERVAL '10 days' + INTERVAL '2 hours',
        NOW() - INTERVAL '10 days' + INTERVAL '2 hours'
    ),
    (
        2,
        2,
        2,
        2,
        NOW() - INTERVAL '9 days' + INTERVAL '1 hour',
        NOW() - INTERVAL '9 days' + INTERVAL '1 hour',
        NOW() - INTERVAL '9 days' + INTERVAL '1 hour'
    ),
    (
        3,
        2,
        3,
        3,
        NOW() - INTERVAL '8 days' + INTERVAL '3 hours',
        NOW() - INTERVAL '8 days' + INTERVAL '3 hours',
        NOW() - INTERVAL '8 days' + INTERVAL '3 hours'
    ),
    (
        4,
        2,
        4,
        4,
        NOW() - INTERVAL '7 days' + INTERVAL '1.5 hours',
        NOW() - INTERVAL '7 days' + INTERVAL '1.5 hours',
        NOW() - INTERVAL '7 days' + INTERVAL '1.5 hours'
    ),
    (
        5,
        2,
        5,
        5,
        NOW() - INTERVAL '6 days' + INTERVAL '2.5 hours',
        NOW() - INTERVAL '6 days' + INTERVAL '2.5 hours',
        NOW() - INTERVAL '6 days' + INTERVAL '2.5 hours'
    ),
    (
        6,
        2,
        6,
        6,
        NOW() - INTERVAL '5 days' + INTERVAL '2 hours',
        NOW() - INTERVAL '5 days' + INTERVAL '2 hours',
        NOW() - INTERVAL '5 days' + INTERVAL '2 hours'
    ),
    (
        7,
        2,
        7,
        7,
        NOW() - INTERVAL '4 days' + INTERVAL '1 hour',
        NOW() - INTERVAL '4 days' + INTERVAL '1 hour',
        NOW() - INTERVAL '4 days' + INTERVAL '1 hour'
    ),
    (
        8,
        2,
        8,
        8,
        NOW() - INTERVAL '3 days' + INTERVAL '3 hours',
        NOW() - INTERVAL '3 days' + INTERVAL '3 hours',
        NOW() - INTERVAL '3 days' + INTERVAL '3 hours'
    ),
    (
        9,
        2,
        9,
        9,
        NOW() - INTERVAL '2 days' + INTERVAL '1.5 hours',
        NOW() - INTERVAL '2 days' + INTERVAL '1.5 hours',
        NOW() - INTERVAL '2 days' + INTERVAL '1.5 hours'
    ),
    (
        10,
        2,
        10,
        10,
        NOW() - INTERVAL '1 days' + INTERVAL '2.5 hours',
        NOW() - INTERVAL '1 days' + INTERVAL '2.5 hours',
        NOW() - INTERVAL '1 days' + INTERVAL '2.5 hours'
    );
-- ---------------------------
-- VehicleUsageRecord (10)  -- references BookingId, VehicleId, CoOwnerId, CheckInId, CheckOutId
-- NOTE: schema has NO CreatedAt/UpdatedAt for this table, so we don't insert those columns.
-- ---------------------------
INSERT INTO "VehicleUsageRecord" (
        BookingId,
        VehicleId,
        CoOwnerId,
        CheckInId,
        CheckOutId,
        StartTime,
        EndTime,
        DurationHours,
        DistanceKm
    )
VALUES (
        1,
        1,
        1,
        1,
        1,
        NOW() - INTERVAL '2 hours',
        NOW(),
        2.0,
        20
    ),
    (
        2,
        2,
        4,
        2,
        2,
        NOW() - INTERVAL '1 hour',
        NOW(),
        1.0,
        10
    ),
    (
        3,
        3,
        5,
        3,
        3,
        NOW() - INTERVAL '3 hours',
        NOW(),
        3.0,
        30
    ),
    (
        4,
        4,
        1,
        4,
        4,
        NOW() - INTERVAL '1.5 hours',
        NOW(),
        1.5,
        15
    ),
    (
        5,
        5,
        3,
        5,
        5,
        NOW() - INTERVAL '2.5 hours',
        NOW(),
        2.5,
        25
    ),
    (
        6,
        6,
        6,
        6,
        6,
        NOW() - INTERVAL '4 hours',
        NOW() - INTERVAL '2 hours',
        2.0,
        40
    ),
    (
        7,
        7,
        7,
        7,
        7,
        NOW() - INTERVAL '6 hours',
        NOW() - INTERVAL '5 hours',
        1.0,
        12
    ),
    (
        8,
        8,
        8,
        8,
        8,
        NOW() - INTERVAL '8 hours',
        NOW() - INTERVAL '6 hours',
        2.0,
        35
    ),
    (
        9,
        9,
        9,
        9,
        9,
        NOW() - INTERVAL '12 hours',
        NOW() - INTERVAL '11 hours',
        1.0,
        9
    ),
    (
        10,
        10,
        10,
        10,
        10,
        NOW() - INTERVAL '24 hours',
        NOW() - INTERVAL '23 hours',
        1.0,
        18
    );
-- ---------------------------
-- MaintenanceCost (10)
-- ---------------------------
INSERT INTO "MaintenanceCost" (
        VehicleId,
        BookingId,
        MaintenanceTypeEnum,
        Description,
        Cost,
        IsPaid,
        ServiceProvider,
        ServiceDate,
        OdometerReading,
        ImageUrl,
        CreatedAt
    )
VALUES (
        1,
        1,
        1,
        'Battery replacement',
        100000,
        TRUE,
        'EV Service Center',
        '2023-02-01',
        12000,
        NULL,
        NOW()
    ),
    (
        2,
        2,
        2,
        'Tire change',
        50000,
        TRUE,
        'Bike Service',
        '2022-07-01',
        3500,
        NULL,
        NOW()
    ),
    (
        3,
        3,
        1,
        'Brake service',
        120000,
        FALSE,
        'Scooter Shop',
        '2024-04-01',
        500,
        NULL,
        NOW()
    ),
    (
        4,
        4,
        3,
        'Software update',
        80000,
        TRUE,
        'Tesla Center',
        '2023-10-01',
        8000,
        NULL,
        NOW()
    ),
    (
        5,
        5,
        2,
        'General maintenance',
        60000,
        TRUE,
        'VinFast Service',
        '2022-01-01',
        9000,
        NULL,
        NOW()
    ),
    (
        6,
        6,
        1,
        'Battery check',
        110000,
        TRUE,
        'EV Service Center',
        '2024-01-15',
        4000,
        NULL,
        NOW()
    ),
    (
        7,
        7,
        2,
        'Tire rotation',
        60000,
        TRUE,
        'Bike Shop',
        '2024-02-20',
        2500,
        NULL,
        NOW()
    ),
    (
        8,
        8,
        3,
        'Brake pad change',
        130000,
        TRUE,
        'Mechanic',
        '2024-03-05',
        7500,
        NULL,
        NOW()
    ),
    (
        9,
        9,
        1,
        'Software patch',
        90000,
        TRUE,
        'Software Service',
        '2024-04-10',
        9000,
        NULL,
        NOW()
    ),
    (
        10,
        10,
        2,
        'Full inspection',
        70000,
        TRUE,
        'Inspection Co',
        '2024-05-12',
        10000,
        NULL,
        NOW()
    );
-- ---------------------------
-- FundAddition (10)
-- ---------------------------
INSERT INTO "FundAddition" (
        FundId,
        CoOwnerId,
        Amount,
        PaymentMethodEnum,
        TransactionId,
        Description,
        StatusEnum,
        CreatedAt
    )
VALUES (1, 1, 500000, 1, 'TXN001', 'Initial deposit', 1, NOW()),
    (2, 3, 300000, 2, 'TXN002', 'Monthly top-up', 1, NOW()),
    (3, 4, 200000, 1, 'TXN003', 'Maintenance fund', 1, NOW()),
    (4, 5, 400000, 2, 'TXN004', 'Upgrade fund', 1, NOW()),
    (5, 1, 250000, 1, 'TXN005', 'Emergency fund', 1, NOW()),
    (6, 6, 350000, 2, 'TXN006', 'Annual bonus', 1, NOW()),
    (7, 7, 450000, 1, 'TXN007', 'Special event', 1, NOW()),
    (8, 8, 150000, 2, 'TXN008', 'Minor repair', 1, NOW()),
    (9, 9, 550000, 1, 'TXN009', 'Major repair', 1, NOW()),
    (10, 10, 650000, 2, 'TXN010', 'Upgrade', 1, NOW());
-- ---------------------------
-- FundUsage (10)
-- ---------------------------
INSERT INTO "FundUsage" (
        FundId,
        UsageTypeEnum,
        Amount,
        Description,
        ImageUrl,
        MaintenanceCostId,
        CreatedAt
    )
VALUES (1, 1, 100000, 'Battery replacement', NULL, 1, NOW()),
    (2, 2, 50000, 'Tire change', NULL, 2, NOW()),
    (3, 1, 120000, 'Brake service', NULL, 3, NOW()),
    (4, 3, 80000, 'Software update', NULL, 4, NOW()),
    (5, 2, 60000, 'General maintenance', NULL, 5, NOW()),
    (6, 1, 110000, 'Battery check', NULL, 6, NOW()),
    (7, 2, 60000, 'Tire rotation', NULL, 7, NOW()),
    (8, 3, 130000, 'Brake pad change', NULL, 8, NOW()),
    (9, 1, 90000, 'Software patch', NULL, 9, NOW()),
    (10, 2, 70000, 'Full inspection', NULL, 10, NOW());
-- ---------------------------
-- FundUsageVote (10)
-- ---------------------------
INSERT INTO "FundUsageVote" (FundUsageId, UserId, IsAgree, CreatedAt)
VALUES (1, 1, TRUE, NOW()),
    (1, 3, TRUE, NOW()),
    (2, 4, FALSE, NOW()),
    (3, 5, TRUE, NOW()),
    (4, 1, TRUE, NOW()),
    (5, 2, TRUE, NOW()),
    (6, 6, TRUE, NOW()),
    (7, 7, FALSE, NOW()),
    (8, 8, TRUE, NOW()),
    (9, 9, TRUE, NOW());
-- ---------------------------
-- Payment (10)
-- ---------------------------
INSERT INTO "Payment" (
        UserId,
        Amount,
        TransactionId,
        PaymentGateway,
        StatusEnum,
        PaidAt,
        CreatedAt,
        FundAdditionId
    )
VALUES (1, 500000, 'PAY001', 'VNPAY', 1, NOW(), NOW(), 1),
    (3, 300000, 'PAY002', 'MOMO', 1, NOW(), NOW(), 2),
    (4, 200000, 'PAY003', 'VNPAY', 1, NOW(), NOW(), 3),
    (5, 400000, 'PAY004', 'MOMO', 1, NOW(), NOW(), 4),
    (1, 250000, 'PAY005', 'VNPAY', 1, NOW(), NOW(), 5),
    (6, 350000, 'PAY006', 'MOMO', 1, NOW(), NOW(), 6),
    (7, 450000, 'PAY007', 'VNPAY', 1, NOW(), NOW(), 7),
    (8, 150000, 'PAY008', 'MOMO', 1, NOW(), NOW(), 8),
    (9, 550000, 'PAY009', 'VNPAY', 1, NOW(), NOW(), 9),
    (10, 650000, 'PAY010', 'MOMO', 1, NOW(), NOW(), 10);
-- ---------------------------
-- NotificationEntity (10)
-- ---------------------------
INSERT INTO "NotificationEntity" (NotificationType, AdditionalDataJson, CreatedAt)
VALUES ('Booking', '{}', NOW()),
    ('Fund', '{}', NOW()),
    ('Maintenance', '{}', NOW()),
    ('Upgrade', '{}', NOW()),
    ('General', '{}', NOW()),
    ('Alert', '{}', NOW()),
    ('Reminder', '{}', NOW()),
    ('Warning', '{}', NOW()),
    ('Info', '{}', NOW()),
    ('Critical', '{}', NOW());
-- ---------------------------
-- UserNotification (10)
-- ---------------------------
INSERT INTO "UserNotification" (NotificationId, UserId, ReadAt)
VALUES (1, 1, NOW()),
    (2, 3, NOW()),
    (3, 4, NOW()),
    (4, 5, NOW()),
    (5, 1, NOW()),
    (1, 6, NOW()),
    (2, 7, NOW()),
    (3, 8, NOW()),
    (4, 9, NOW()),
    (5, 10, NOW());
-- ---------------------------
-- UserRefreshToken (10)
-- ---------------------------
INSERT INTO "UserRefreshToken" (UserId, RefreshToken, ExpiresAt)
VALUES (1, 'refresh1', NOW() + INTERVAL '30 days'),
    (2, 'refresh2', NOW() + INTERVAL '30 days'),
    (3, 'refresh3', NOW() + INTERVAL '30 days'),
    (4, 'refresh4', NOW() + INTERVAL '30 days'),
    (5, 'refresh5', NOW() + INTERVAL '30 days'),
    (6, 'refresh6', NOW() + INTERVAL '30 days'),
    (7, 'refresh7', NOW() + INTERVAL '30 days'),
    (8, 'refresh8', NOW() + INTERVAL '30 days'),
    (9, 'refresh9', NOW() + INTERVAL '30 days'),
    (10, 'refresh10', NOW() + INTERVAL '30 days');
-- ---------------------------
-- VehicleVerificationHistory (10)
-- ---------------------------
INSERT INTO "VehicleVerificationHistory" (
        VehicleId,
        StaffId,
        StatusEnum,
        Notes,
        ImagesJson,
        CreatedAt
    )
VALUES (1, 2, 1, 'Verified OK', '{}', NOW()),
    (2, 2, 1, 'Verified OK', '{}', NOW()),
    (3, 2, 2, 'Minor issue', '{}', NOW()),
    (4, 2, 1, 'Verified OK', '{}', NOW()),
    (5, 2, 1, 'Verified OK', '{}', NOW()),
    (6, 2, 1, 'Verified OK', '{}', NOW()),
    (7, 2, 2, 'Minor issue', '{}', NOW()),
    (8, 2, 1, 'Verified OK', '{}', NOW()),
    (9, 2, 1, 'Verified OK', '{}', NOW()),
    (10, 2, 1, 'Verified OK', '{}', NOW());
-- ---------------------------
-- VehicleUpgradeProposal (10)
-- ---------------------------
INSERT INTO "VehicleUpgradeProposal" (
        VehicleId,
        UpgradeType,
        Title,
        Description,
        EstimatedCost,
        Justification,
        ImageUrl,
        VendorName,
        VendorContact,
        ProposedInstallationDate,
        EstimatedDurationDays,
        ProposedByUserId,
        ProposedAt,
        Status,
        ApprovedAt,
        RejectedAt,
        CancelledAt,
        IsExecuted,
        ExecutedAt,
        ActualCost,
        ExecutionNotes,
        FundUsageId,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        1,
        1,
        'Install fast charger',
        'Upgrade to 22kW charger',
        2000000,
        'Faster charging needed',
        NULL,
        'EV Charger Co',
        '0909000001',
        NOW() + INTERVAL '7 days',
        2,
        1,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        1,
        NOW(),
        NOW()
    ),
    (
        2,
        2,
        'Add GPS',
        'Install GPS tracker',
        1000000,
        'For security',
        NULL,
        'GPS Co',
        '0909000002',
        NOW() + INTERVAL '10 days',
        1,
        3,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        2,
        NOW(),
        NOW()
    ),
    (
        3,
        1,
        'Upgrade battery',
        'Replace with 50kWh battery',
        5000000,
        'Longer range',
        NULL,
        'Battery Co',
        '0909000003',
        NOW() + INTERVAL '15 days',
        3,
        4,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        3,
        NOW(),
        NOW()
    ),
    (
        4,
        2,
        'Add dashcam',
        'Install dash camera',
        1500000,
        'Safety',
        NULL,
        'Camera Co',
        '0909000004',
        NOW() + INTERVAL '5 days',
        1,
        5,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        4,
        NOW(),
        NOW()
    ),
    (
        5,
        2,
        'Upgrade tires',
        'Install all-weather tires',
        1200000,
        'Better grip',
        NULL,
        'Tire Co',
        '0909000005',
        NOW() + INTERVAL '12 days',
        1,
        1,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        5,
        NOW(),
        NOW()
    ),
    (
        6,
        3,
        'Add solar roof',
        'Install solar panel roof',
        3000000,
        'Eco-friendly',
        NULL,
        'Solar Co',
        '0909000006',
        NOW() + INTERVAL '8 days',
        2,
        6,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        6,
        NOW(),
        NOW()
    ),
    (
        7,
        4,
        'Upgrade seats',
        'Install leather seats',
        2500000,
        'Comfort',
        NULL,
        'Seat Co',
        '0909000007',
        NOW() + INTERVAL '9 days',
        1,
        7,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        7,
        NOW(),
        NOW()
    ),
    (
        8,
        5,
        'Add air purifier',
        'Install air purifier',
        800000,
        'Health',
        NULL,
        'Air Co',
        '0909000008',
        NOW() + INTERVAL '11 days',
        1,
        8,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        8,
        NOW(),
        NOW()
    ),
    (
        9,
        6,
        'Upgrade sound',
        'Install premium sound system',
        1800000,
        'Entertainment',
        NULL,
        'Sound Co',
        '0909000009',
        NOW() + INTERVAL '13 days',
        2,
        9,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        9,
        NOW(),
        NOW()
    ),
    (
        10,
        7,
        'Add rear camera',
        'Install rear view camera',
        900000,
        'Safety',
        NULL,
        'Camera Co',
        '0909000010',
        NOW() + INTERVAL '14 days',
        1,
        10,
        NOW(),
        'Pending',
        NULL,
        NULL,
        NULL,
        FALSE,
        NULL,
        NULL,
        NULL,
        10,
        NOW(),
        NOW()
    );
-- ---------------------------
-- VehicleUpgradeVote (10)
-- ---------------------------
INSERT INTO "VehicleUpgradeVote" (ProposalId, UserId, IsAgree, Comments, VotedAt)
VALUES (1, 1, TRUE, 'Good idea', NOW()),
    (2, 4, FALSE, 'Not needed', NOW()),
    (3, 5, TRUE, 'Approve', NOW()),
    (4, 1, TRUE, 'OK', NOW()),
    (5, 2, TRUE, 'Looks good', NOW()),
    (6, 6, TRUE, 'Great', NOW()),
    (7, 7, FALSE, 'Not required', NOW()),
    (8, 8, TRUE, 'Nice', NOW()),
    (9, 9, TRUE, 'Superb', NOW()),
    (10, 10, FALSE, 'No', NOW());
-- ---------------------------
-- OwnershipChangeRequest & OwnershipHistory (sample rows)
-- ---------------------------
INSERT INTO "OwnershipChangeRequest" (
        VehicleId,
        ProposedByUserId,
        Reason,
        StatusEnum,
        CreatedAt,
        UpdatedAt,
        FinalizedAt,
        RequiredApprovals,
        CurrentApprovals
    )
VALUES (1, 1, 'Transfer share', 1, NOW(), NOW(), NULL, 2, 0),
    (2, 2, 'Buy out', 1, NOW(), NOW(), NULL, 2, 0);
INSERT INTO "OwnershipHistory" (
        VehicleId,
        CoOwnerId,
        UserId,
        OwnershipChangeRequestId,
        PreviousPercentage,
        NewPercentage,
        PercentageChange,
        PreviousInvestment,
        NewInvestment,
        InvestmentChange,
        ChangedAt
    )
VALUES (
        1,
        1,
        1,
        NULL,
        100.0,
        50.0,
        -50.0,
        350000000,
        350000000,
        0,
        NOW()
    ),
    (
        1,
        3,
        3,
        NULL,
        0,
        50.0,
        50.0,
        0,
        350000000,
        350000000,
        NOW()
    );
-- ---------------------------
-- Configuration, UserReminderPreference, BookingReminderLog, FileUpload, DrivingLicense
-- ---------------------------
INSERT INTO "Configuration" (Key, Value, Description, UpdatedAt)
VALUES ('site.name', 'EV Co-Ownership', 'Site title', NOW());
INSERT INTO "UserReminderPreference" (
        UserId,
        HoursBeforeBooking,
        Enabled,
        CreatedAt,
        UpdatedAt
    )
VALUES (1, 2, TRUE, NOW(), NOW());
INSERT INTO "BookingReminderLog" (
        BookingId,
        UserId,
        SentAt,
        BookingStartTime,
        HoursBeforeBooking,
        Success
    )
VALUES (1, 1, NOW(), NOW() + INTERVAL '1 day', 24, TRUE);
INSERT INTO "FileUpload" (Data, FileName, MimeType, UploadedAt)
VALUES (NULL, 'sample.txt', 'text/plain', NOW());
INSERT INTO "DrivingLicense" (
        CoOwnerId,
        LicenseNumber,
        IssuedBy,
        IssueDate,
        ExpiryDate,
        LicenseImageUrl,
        CreatedAt,
        UpdatedAt
    )
VALUES (
        1,
        'DL001',
        'HCM',
        '2015-01-01',
        '2030-01-01',
        'license1.png',
        NOW(),
        NOW()
    ),
    (
        2,
        'DL002',
        'HN',
        '2016-02-01',
        '2031-02-01',
        'license2.png',
        NOW(),
        NOW()
    ),
    (
        3,
        'DL003',
        'DN',
        '2017-03-01',
        '2032-03-01',
        'license3.png',
        NOW(),
        NOW()
    ),
    (
        4,
        'DL004',
        'CT',
        '2018-04-01',
        '2033-04-01',
        'license4.png',
        NOW(),
        NOW()
    ),
    (
        5,
        'DL005',
        'HCM',
        '2019-05-01',
        '2034-05-01',
        'license5.png',
        NOW(),
        NOW()
    ),
    (
        6,
        'DL006',
        'HN',
        '2020-06-01',
        '2035-06-01',
        'license6.png',
        NOW(),
        NOW()
    ),
    (
        7,
        'DL007',
        'DN',
        '2021-07-01',
        '2036-07-01',
        'license7.png',
        NOW(),
        NOW()
    ),
    (
        8,
        'DL008',
        'CT',
        '2022-08-01',
        '2037-08-01',
        'license8.png',
        NOW(),
        NOW()
    ),
    (
        9,
        'DL009',
        'HCM',
        '2023-09-01',
        '2038-09-01',
        'license9.png',
        NOW(),
        NOW()
    ),
    (
        10,
        'DL010',
        'HN',
        '2024-10-01',
        '2039-10-01',
        'license10.png',
        NOW(),
        NOW()
    );
-- ---------------------------
-- Final note
-- ---------------------------
-- Script complete.
-- Run with: psql -U <user> -d <db> -f /path/to/master_sample_data_fixed.sql