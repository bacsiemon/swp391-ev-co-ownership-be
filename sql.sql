-- ==============================================================================
-- EV CO-OWNERSHIP DATABASE SCHEMA
-- ==============================================================================
-- Compatible with: PostgreSQL and Entity Framework Core
-- Last updated: 2025-11-20
-- Description: Complete database schema for Electric Vehicle Co-Ownership Platform
-- ==============================================================================

DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;

-- ==============================================================================
-- ENUM MAPPINGS DOCUMENTATION
-- ==============================================================================
-- All enums use 0-based indexing for consistency
-- Stored as INTEGER values in database tables
-- ------------------------------------------------------------------------------

-- USER RELATED ENUMS
-- user_role_enum:           0=CoOwner, 1=Staff, 2=Admin
-- user_status_enum:         0=Active, 1=Inactive, 2=Suspended

-- VEHICLE RELATED ENUMS  
-- vehicle_status_enum:      0=Available, 1=InUse, 2=Maintenance, 3=Unavailable
-- vehicle_verification_status_enum: 0=Pending, 1=VerificationRequested, 2=RequiresRecheck, 3=Verified, 4=Rejected
-- condition_type_enum:      0=Excellent, 1=Good, 2=Fair, 3=Poor, 4=Damaged

-- BOOKING RELATED ENUMS
-- booking_status_enum:      0=Pending, 1=Confirmed, 2=Active, 3=Completed, 4=Cancelled

-- PAYMENT & FUND RELATED ENUMS
-- payment_method_enum:      0=BankTransfer, 1=CreditCard, 2=DebitCard, 3=Cash
-- fund_addition_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
-- payment_status_enum:      0=Pending, 1=Completed, 2=Failed, 3=Refunded

-- MAINTENANCE & USAGE RELATED ENUMS
-- maintenance_type_enum:    0=Routine, 1=Repair, 2=Emergency, 3=Upgrade
-- usage_type_enum:          0=Maintenance, 1=Insurance, 2=Fuel, 3=Parking, 4=Other

-- DRIVING LICENSE VERIFICATION ENUMS
-- driving_license_verification_status_enum: 0=Pending, 1=Verified, 2=Rejected, 3=Expired

-- UPGRADE RELATED ENUMS
-- upgrade_type_enum:        0=BatteryUpgrade, 1=SoftwareUpdate, 2=HardwareInstallation, 3=Maintenance, 4=Cosmetic

-- GROUP RELATED ENUMS
-- group_status_enum:        0=Active, 1=Inactive, 2=Disbanded
-- group_type_enum:          0=VehicleCoOwnership, 1=Community, 2=Business
-- group_member_role_enum:   0=Member, 1=Admin, 2=Owner
-- group_member_status_enum: 0=Active, 1=Pending, 2=Removed
-- group_vote_type_enum:     0=Maintenance, 1=Purchase, 2=Upgrade, 3=General
-- group_vote_status_enum:   0=Active, 1=Completed, 2=Cancelled
-- group_fund_status_enum:   0=Active, 1=Completed, 2=Cancelled

-- ==============================================================================
-- CORE SYSTEM TABLES
-- ==============================================================================
-- Tables with no dependencies on other tables
-- ------------------------------------------------------------------------------

-- System configuration table
CREATE TABLE configurations (
	key VARCHAR(100) PRIMARY KEY,
	value TEXT NOT NULL,
	description TEXT,
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Main users table
CREATE TABLE users (
	id SERIAL PRIMARY KEY,
	email VARCHAR(255) UNIQUE NOT NULL,
	normalized_email VARCHAR(255) UNIQUE NOT NULL,
	password_hash VARCHAR(255) NOT NULL,
	password_salt VARCHAR(255) NOT NULL,
	first_name VARCHAR(100) NOT NULL,
	last_name VARCHAR(100) NOT NULL,
	phone VARCHAR(20),
	date_of_birth DATE,
	address TEXT,
	profile_image_url VARCHAR(500),
	role_enum INTEGER DEFAULT 0,                    -- user_role_enum: 0=CoOwner, 1=Staff, 2=Admin
	status_enum INTEGER DEFAULT 0,                   -- user_status_enum: 0=Active, 1=Inactive, 2=Suspended
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- User refresh tokens for JWT authentication
CREATE TABLE user_refresh_tokens (
	user_id INTEGER PRIMARY KEY REFERENCES users(id),
	refresh_token VARCHAR(255) NOT NULL,
	expires_at TIMESTAMP NOT NULL
);

-- Co-owner specific data
CREATE TABLE co_owners (
	user_id INTEGER PRIMARY KEY REFERENCES users(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle fund management
CREATE TABLE funds (
	id SERIAL PRIMARY KEY,
	current_balance DECIMAL(15, 2) DEFAULT 0,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle stations/locations
CREATE TABLE vehicle_stations (
	id SERIAL PRIMARY KEY,
	name VARCHAR(200) NOT NULL,
	description TEXT,
	address TEXT NOT NULL,
	contact_number VARCHAR(20),
	location_latitude DECIMAL(10, 8) NOT NULL,
	location_longitude DECIMAL(11, 8) NOT NULL,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- ==============================================================================
-- VEHICLE & RELATED TABLES
-- ==============================================================================
-- Tables for vehicle management, ownership, and condition tracking
-- ------------------------------------------------------------------------------

-- Main vehicles table
CREATE TABLE vehicles (
	id SERIAL PRIMARY KEY,
	name VARCHAR(200) NOT NULL,
	description TEXT,
	brand VARCHAR(100) NOT NULL,
	model VARCHAR(100) NOT NULL,
	year INTEGER NOT NULL,
	vin VARCHAR(17) UNIQUE NOT NULL,
	license_plate VARCHAR(20) UNIQUE NOT NULL,
	color VARCHAR(50),
	battery_capacity DECIMAL(6, 2),
	range_km INTEGER,
	purchase_date DATE NOT NULL,
	purchase_price DECIMAL(15, 2) NOT NULL,
	warranty_until DATE,
	distance_travelled INTEGER DEFAULT 0,
	status_enum INTEGER DEFAULT 0,                   -- vehicle_status_enum: 0=Available, 1=InUse, 2=Maintenance, 3=Unavailable
	verification_status_enum INTEGER DEFAULT 0,      -- vehicle_verification_status_enum: 0=Pending, 1=VerificationRequested, 2=RequiresRecheck, 3=Verified, 4=Rejected
	location_latitude DECIMAL(10, 8),
	location_longitude DECIMAL(11, 8),
	created_by INTEGER REFERENCES users(id),
	fund_id INTEGER REFERENCES funds(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Driving licenses with enhanced verification
CREATE TABLE driving_licenses (
	id SERIAL PRIMARY KEY,
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	license_number VARCHAR(50) UNIQUE NOT NULL,
	issued_by VARCHAR(100) NOT NULL,
	issue_date DATE NOT NULL,
	expiry_date DATE,
	license_image_url VARCHAR(500),
	verification_status INTEGER DEFAULT 0,           -- driving_license_verification_status_enum: 0=Pending, 1=Verified, 2=Rejected, 3=Expired
	reject_reason TEXT,
	verified_by_user_id INTEGER REFERENCES users(id),
	verified_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle co-ownership relationships
CREATE TABLE vehicle_co_owners (
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	vehicle_id INTEGER REFERENCES vehicles(id),
	PRIMARY KEY (co_owner_id, vehicle_id),
	ownership_percentage DECIMAL(5, 2) NOT NULL,
	investment_amount DECIMAL(15, 2) NOT NULL,
	status_enum INTEGER DEFAULT 0,                   -- 0=Active, 1=Pending, 2=Terminated
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle condition reports
CREATE TABLE vehicle_conditions (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	reported_by INTEGER REFERENCES users(id),
	condition_type_enum INTEGER,                     -- condition_type_enum: 0=Excellent, 1=Good, 2=Fair, 3=Poor, 4=Damaged
	description TEXT,
	photo_urls TEXT,
	odometer_reading INTEGER,
	fuel_level DECIMAL(5, 2),
	damage_reported BOOLEAN DEFAULT FALSE,
	created_at TIMESTAMP DEFAULT NOW()
);

-- ==============================================================================
-- BOOKING & USAGE TABLES
-- ==============================================================================
-- Tables for vehicle booking, check-in/out, and usage tracking
-- ------------------------------------------------------------------------------

-- Vehicle bookings
CREATE TABLE bookings (
	id SERIAL PRIMARY KEY,
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	vehicle_id INTEGER REFERENCES vehicles(id),
	start_time TIMESTAMP NOT NULL,
	end_time TIMESTAMP NOT NULL,
	purpose VARCHAR(500),
	status_enum INTEGER DEFAULT 0,                   -- booking_status_enum: 0=Pending, 1=Confirmed, 2=Active, 3=Completed, 4=Cancelled
	approved_by INTEGER REFERENCES users(id),
	total_cost DECIMAL(10, 2),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Check-in records
CREATE TABLE check_ins (
	id SERIAL PRIMARY KEY,
	booking_id INTEGER REFERENCES bookings(id),
	staff_id INTEGER REFERENCES users(id),
	vehicle_station_id INTEGER REFERENCES vehicle_stations(id),
	vehicle_condition_id INTEGER REFERENCES vehicle_conditions(id),
	check_time TIMESTAMP NOT NULL,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Check-out records
CREATE TABLE check_outs (
	id SERIAL PRIMARY KEY,
	booking_id INTEGER REFERENCES bookings(id),
	staff_id INTEGER REFERENCES users(id),
	vehicle_station_id INTEGER REFERENCES vehicle_stations(id),
	vehicle_condition_id INTEGER REFERENCES vehicle_conditions(id),
	check_time TIMESTAMP NOT NULL,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle usage tracking (complete trip records)
CREATE TABLE vehicle_usage_records (
	id SERIAL PRIMARY KEY,
	booking_id INTEGER REFERENCES bookings(id),
	vehicle_id INTEGER REFERENCES vehicles(id),
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	check_in_id INTEGER REFERENCES check_ins(id),
	check_out_id INTEGER REFERENCES check_outs(id),
	start_time TIMESTAMP NOT NULL,
	end_time TIMESTAMP NOT NULL,
	duration_hours DECIMAL(5, 2) NOT NULL,
	distance_km INTEGER,
	battery_used_percent INTEGER,
	odometer_start INTEGER,
	odometer_end INTEGER,
	battery_level_start INTEGER,
	battery_level_end INTEGER,
	booking_cost DECIMAL(10, 2),
	late_fee DECIMAL(10, 2),
	damage_fee DECIMAL(10, 2),
	total_cost DECIMAL(10, 2),
	purpose VARCHAR(500),
	notes TEXT,
	was_qr_scanned BOOLEAN,
	assisting_staff_id INTEGER REFERENCES users(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- ==============================================================================
-- FINANCIAL TABLES
-- ==============================================================================
-- Tables for fund management, payments, and financial tracking
-- ------------------------------------------------------------------------------

-- Fund additions (money coming in)
CREATE TABLE fund_additions (
	id SERIAL PRIMARY KEY,
	fund_id INTEGER REFERENCES funds(id),
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	amount DECIMAL(15, 2) NOT NULL,
	payment_method_enum INTEGER,                     -- payment_method_enum: 0=BankTransfer, 1=CreditCard, 2=DebitCard, 3=Cash
	transaction_id VARCHAR(100),
	description TEXT,
	status_enum INTEGER DEFAULT 0,                   -- fund_addition_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
	created_at TIMESTAMP DEFAULT NOW()
);

-- Fund usage (money going out)
CREATE TABLE fund_usage (
	id SERIAL PRIMARY KEY,
	fund_id INTEGER REFERENCES funds(id),
	usage_type_enum INTEGER,                         -- usage_type_enum: 0=Maintenance, 1=Insurance, 2=Fuel, 3=Parking, 4=Other
	amount DECIMAL(15, 2) NOT NULL,
	description TEXT NOT NULL,
	image_url VARCHAR(500),
	maintenance_cost_id INTEGER,
	created_at TIMESTAMP DEFAULT NOW()
);

-- Fund usage voting
CREATE TABLE fund_usage_votes (
	fund_usage_id INTEGER REFERENCES fund_usage(id) NOT NULL,
	user_id INTEGER REFERENCES users(id) NOT NULL,
	is_agree BOOLEAN NOT NULL,
	created_at TIMESTAMP DEFAULT NOW(),
	PRIMARY KEY (fund_usage_id, user_id)
);

-- Payment processing
CREATE TABLE payments (
	id SERIAL PRIMARY KEY,
	user_id INTEGER REFERENCES users(id),
	amount DECIMAL(10, 2) NOT NULL,
	transaction_id VARCHAR(100) UNIQUE,
	payment_gateway VARCHAR(50),
	status_enum INTEGER DEFAULT 0,                   -- payment_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
	paid_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	fund_addition_id INTEGER REFERENCES fund_additions(id)
);

-- ==============================================================================
-- MAINTENANCE & UPGRADE TABLES
-- ==============================================================================
-- Tables for vehicle maintenance and upgrade management
-- ------------------------------------------------------------------------------

-- Maintenance costs
CREATE TABLE maintenance_costs (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	booking_id INTEGER REFERENCES bookings(id),
	maintenance_type_enum INTEGER,                   -- maintenance_type_enum: 0=Routine, 1=Repair, 2=Emergency, 3=Upgrade
	description TEXT NOT NULL,
	cost DECIMAL(10, 2) NOT NULL,
	is_paid BOOLEAN DEFAULT FALSE,
	service_provider VARCHAR(200),
	service_date DATE NOT NULL,
	odometer_reading INTEGER,
	image_url VARCHAR(500),
	created_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle upgrade proposals
CREATE TABLE vehicle_upgrade_proposals (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	upgrade_type_enum INTEGER NOT NULL,              -- upgrade_type_enum: 0=BatteryUpgrade, 1=SoftwareUpdate, 2=HardwareInstallation, 3=Maintenance, 4=Cosmetic
	title VARCHAR(200) NOT NULL,
	description TEXT NOT NULL,
	estimated_cost DECIMAL(15, 2) NOT NULL,
	justification TEXT,
	image_url VARCHAR(500),
	vendor_name VARCHAR(200),
	vendor_contact VARCHAR(100),
	proposed_installation_date DATE,
	estimated_duration_days INTEGER,
	proposed_by_user_id INTEGER REFERENCES users(id),
	proposed_at TIMESTAMP NOT NULL,
	status VARCHAR(20) DEFAULT 'Pending',            -- Pending, Approved, Rejected, Cancelled
	approved_at TIMESTAMP,
	rejected_at TIMESTAMP,
	cancelled_at TIMESTAMP,
	is_executed BOOLEAN DEFAULT FALSE,
	executed_at TIMESTAMP,
	actual_cost DECIMAL(15, 2),
	execution_notes TEXT,
	fund_usage_id INTEGER REFERENCES fund_usage(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Vehicle upgrade voting
CREATE TABLE vehicle_upgrade_votes (
	proposal_id INTEGER REFERENCES vehicle_upgrade_proposals(id),
	user_id INTEGER REFERENCES users(id),
	is_agree BOOLEAN NOT NULL,
	comments TEXT,
	voted_at TIMESTAMP DEFAULT NOW(),
	PRIMARY KEY (proposal_id, user_id)
);

-- ==============================================================================
-- GROUP MANAGEMENT TABLES
-- ==============================================================================
-- Tables for managing co-ownership groups and group activities
-- ------------------------------------------------------------------------------

-- Groups table
CREATE TABLE groups (
	id SERIAL PRIMARY KEY,
	name VARCHAR(200) NOT NULL,
	description TEXT,
	created_by INTEGER REFERENCES users(id),
	status_enum INTEGER DEFAULT 0,                   -- group_status_enum: 0=Active, 1=Inactive, 2=Disbanded
	max_members INTEGER DEFAULT 10,
	group_type_enum INTEGER DEFAULT 0,               -- group_type_enum: 0=VehicleCoOwnership, 1=Community, 2=Business
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Group members
CREATE TABLE group_members (
	group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
	user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
	role_enum INTEGER DEFAULT 0,                     -- group_member_role_enum: 0=Member, 1=Admin, 2=Owner
	joined_at TIMESTAMP DEFAULT NOW(),
	investment_amount DECIMAL(15, 2) DEFAULT 0,
	ownership_percentage DECIMAL(5, 2) DEFAULT 0,
	status_enum INTEGER DEFAULT 0,                   -- group_member_status_enum: 0=Active, 1=Pending, 2=Removed
	PRIMARY KEY (group_id, user_id)
);

-- Group vehicles
CREATE TABLE group_vehicles (
	group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
	vehicle_id INTEGER REFERENCES vehicles(id) ON DELETE CASCADE,
	added_at TIMESTAMP DEFAULT NOW(),
	status_enum INTEGER DEFAULT 0,                   -- 0=Active, 1=Maintenance, 2=Removed
	PRIMARY KEY (group_id, vehicle_id)
);

-- Group votes
CREATE TABLE group_votes (
	id SERIAL PRIMARY KEY,
	group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
	title VARCHAR(200) NOT NULL,
	description TEXT,
	vote_type_enum INTEGER DEFAULT 0,                -- group_vote_type_enum: 0=Maintenance, 1=Purchase, 2=Upgrade, 3=General
	created_by INTEGER REFERENCES users(id),
	start_time TIMESTAMP DEFAULT NOW(),
	end_time TIMESTAMP,
	status_enum INTEGER DEFAULT 0,                   -- group_vote_status_enum: 0=Active, 1=Completed, 2=Cancelled
	required_approval_percentage DECIMAL(5, 2) DEFAULT 60.00,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- Group vote responses
CREATE TABLE group_vote_responses (
	vote_id INTEGER REFERENCES group_votes(id) ON DELETE CASCADE,
	user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
	is_agree BOOLEAN NOT NULL,
	comments TEXT,
	voted_at TIMESTAMP DEFAULT NOW(),
	PRIMARY KEY (vote_id, user_id)
);

-- Group funds
CREATE TABLE group_funds (
	id SERIAL PRIMARY KEY,
	group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
	fund_id INTEGER REFERENCES funds(id),
	target_amount DECIMAL(15, 2) DEFAULT 0,
	current_amount DECIMAL(15, 2) DEFAULT 0,
	purpose TEXT,
	deadline DATE,
	status_enum INTEGER DEFAULT 0,                   -- group_fund_status_enum: 0=Active, 1=Completed, 2=Cancelled
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- ==============================================================================
-- NOTIFICATION & FILE MANAGEMENT TABLES
-- ==============================================================================
-- Tables for notifications and file uploads
-- ------------------------------------------------------------------------------

-- Notification entities
CREATE TABLE notification_entities (
	id SERIAL PRIMARY KEY,
	notification_type TEXT NOT NULL,
	additional_data_json TEXT,
	created_at TIMESTAMP DEFAULT NOW()
);

-- User notifications
CREATE TABLE user_notifications (
	id SERIAL PRIMARY KEY,
	notification_id INTEGER REFERENCES notification_entities(id) ON DELETE CASCADE,
	user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
	read_at TIMESTAMP,
	UNIQUE(notification_id, user_id)
);

-- File uploads
CREATE TABLE file_uploads (
	id SERIAL PRIMARY KEY,
	data BYTEA NOT NULL,
	file_name VARCHAR(255) NOT NULL,
	mime_type VARCHAR(100) NOT NULL,
	uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ==============================================================================
-- SAMPLE DATA - USERS & CONFIGURATION
-- ==============================================================================
-- Initial users and system configuration
-- ------------------------------------------------------------------------------

-- Default admin user (password: Admin123!)
INSERT INTO users (
		email,
		normalized_email,
		password_hash,
		password_salt,
		first_name,
		last_name,
		role_enum,
		status_enum
	)
VALUES (
		'admin@evco.com',
		'ADMIN@EVCO.COM',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1OHLvN2I1pDiS8F1/9vkOKMZw4A1I0QZe',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1O',
		'System',
		'Administrator',
		2,                                           -- Admin
		0                                            -- Active
	);

-- Default staff user (password: Staff123!)
INSERT INTO users (
		email,
		normalized_email,
		password_hash,
		password_salt,
		first_name,
		last_name,
		role_enum,
		status_enum
	)
VALUES (
		'staff@evco.com',
		'STAFF@EVCO.COM',
		'$2a$11$9VF2XRz9Rm.Vb/1UL3aW2PHMwO3J2qEjT9G2/0wlPLNZx5B2J1RZf',
		'$2a$11$9VF2XRz9Rm.Vb/1UL3aW2P',
		'System',
		'Staff',
		1,                                           -- Staff
		0                                            -- Active
	);

-- System configuration values
INSERT INTO configurations (key, value, description)
VALUES (
		'maintenance_check_interval_days',
		'30',
		'Days between routine maintenance checks'
	),
	(
		'booking_advance_limit_days',
		'14',
		'Maximum days in advance users can book vehicles'
	),
	(
		'late_fee_per_hour',
		'50000',
		'Late fee per hour in VND'
	),
	(
		'damage_assessment_threshold',
		'500000',
		'Minimum damage cost that requires assessment in VND'
	),
	(
		'fund_usage_vote_threshold_percent',
		'60',
		'Percentage of co-owners needed to approve fund usage'
	);

-- ==============================================================================
-- SAMPLE DATA - CO-OWNERS & TEST USERS
-- ==============================================================================
-- Sample users for testing purposes
-- ------------------------------------------------------------------------------

-- Sample co-owners (linking to existing system users)
INSERT INTO co_owners (user_id)
VALUES (1),                                          -- Admin
	(2);                                             -- Staff

-- Additional test users
INSERT INTO users (
		email,
		normalized_email,
		password_hash,
		password_salt,
		first_name,
		last_name,
		role_enum,
		status_enum
	)
VALUES (
		'john.doe@example.com',
		'JOHN.DOE@EXAMPLE.COM',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1OHLvN2I1pDiS8F1/9vkOKMZw4A1I0QZe',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1O',
		'John',
		'Doe',
		0,
		0
	),
	(
		'jane.smith@example.com',
		'JANE.SMITH@EXAMPLE.COM',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1OHLvN2I1pDiS8F1/9vkOKMZw4A1I0QZe',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1O',
		'Jane',
		'Smith',
		0,
		0
	),
	(
		'mike.wilson@example.com',
		'MIKE.WILSON@EXAMPLE.COM',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1OHLvN2I1pDiS8F1/9vkOKMZw4A1I0QZe',
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1O',
		'Mike',
		'Wilson',
		0,
		0
	);

-- Co-owners for new test users
INSERT INTO co_owners (user_id)
VALUES (3),                                          -- John
	(4),                                             -- Jane
	(5);                                             -- Mike

-- User refresh tokens
INSERT INTO user_refresh_tokens (user_id, refresh_token, expires_at)
VALUES (
		1,
		'admin_refresh_token_123456789',
		NOW() + INTERVAL '7 days'
	),
	(
		2,
		'staff_refresh_token_987654321',
		NOW() + INTERVAL '7 days'
	);

-- ==============================================================================
-- SAMPLE DATA - FUNDS & STATIONS
-- ==============================================================================
-- Sample funds and vehicle stations
-- ------------------------------------------------------------------------------

-- Sample funds
INSERT INTO funds (current_balance)
VALUES (1500000.00),                                 -- Fund 1: 1.5M VND
	(2300000.00);                                    -- Fund 2: 2.3M VND

-- Vehicle stations
INSERT INTO vehicle_stations (
		name,
		description,
		address,
		contact_number,
		location_latitude,
		location_longitude
	)
VALUES (
		'Central Station',
		'Main vehicle pickup and drop-off point',
		'123 Nguyen Hue Street, District 1, Ho Chi Minh City',
		'+84901234567',
		10.762622,
		106.660172
	),
	(
		'North Station',
		'Secondary station for northern area',
		'456 Le Loi Avenue, District 3, Ho Chi Minh City',
		'+84901234568',
		10.768239,
		106.681885
	);

-- ==============================================================================
-- SAMPLE DATA - VEHICLES & OWNERSHIP
-- ==============================================================================
-- Sample vehicles, licenses, and ownership relationships
-- ------------------------------------------------------------------------------

-- Sample vehicles
INSERT INTO vehicles (
		name,
		description,
		brand,
		model,
		year,
		vin,
		license_plate,
		color,
		battery_capacity,
		range_km,
		purchase_date,
		purchase_price,
		distance_travelled,
		status_enum,
		verification_status_enum,
		created_by,
		fund_id
	)
VALUES (
		'EV Tesla Model 3',
		'Electric sedan for city commuting',
		'Tesla',
		'Model 3',
		2023,
		'1HGBH41JXMN109186',
		'51A-12345',
		'White',
		75.0,
		400,
		'2023-06-15',
		1800000000.00,
		15000,
		0,
		3,
		1,
		1
	),
	(
		'EV VinFast VF8',
		'Vietnamese electric SUV',
		'VinFast',
		'VF8',
		2024,
		'2HGBH41JXMN109187',
		'51B-67890',
		'Blue',
		87.7,
		450,
		'2024-01-20',
		1200000000.00,
		8500,
		0,
		3,
		1,
		2
	);

-- Driving licenses
INSERT INTO driving_licenses (
		co_owner_id,
		license_number,
		issued_by,
		issue_date,
		expiry_date,
		verification_status,
		verified_by_user_id,
		verified_at
	)
VALUES (
		3,
		'DL123456789',
		'Department of Transport HCMC',
		'2020-03-15',
		'2030-03-15',
		1,
		2,
		NOW() - INTERVAL '30 days'
	),
	(
		4,
		'DL987654321',
		'Department of Transport Hanoi',
		'2021-07-20',
		'2031-07-20',
		1,
		2,
		NOW() - INTERVAL '15 days'
	);

-- Vehicle co-ownership relationships
INSERT INTO vehicle_co_owners (
		co_owner_id,
		vehicle_id,
		ownership_percentage,
		investment_amount,
		status_enum
	)
VALUES (3, 1, 55.00, 800000.00, 0),                  -- John owns 55% of Tesla
	(4, 1, 45.00, 700000.00, 0),                     -- Jane owns 45% of Tesla
	(3, 2, 60.00, 1200000.00, 0),                    -- John owns 60% of VinFast
	(5, 2, 40.00, 1100000.00, 0);                    -- Mike owns 40% of VinFast

-- Vehicle conditions
INSERT INTO vehicle_conditions (
		vehicle_id,
		reported_by,
		condition_type_enum,
		description,
		odometer_reading,
		fuel_level,
		damage_reported
	)
VALUES (
		1,
		3,
		1,
		'Vehicle in good condition after recent cleaning',
		15000,
		85.5,
		FALSE
	),
	(
		2,
		5,
		0,
		'Excellent condition, newly purchased',
		8500,
		92.0,
		FALSE
	);

-- ==============================================================================
-- SAMPLE DATA - FINANCIAL TRANSACTIONS
-- ==============================================================================
-- Sample fund additions, usage, and payments
-- ------------------------------------------------------------------------------

-- Fund additions
INSERT INTO fund_additions (
		fund_id,
		co_owner_id,
		amount,
		payment_method_enum,
		transaction_id,
		description,
		status_enum
	)
VALUES (
		1,
		3,
		800000.00,
		0,
		'TXN_001_BANK_001',
		'Initial investment for Tesla Model 3',
		1
	),
	(
		1,
		4,
		700000.00,
		1,
		'TXN_002_CARD_001',
		'Co-investment for Tesla Model 3',
		1
	),
	(
		2,
		3,
		1200000.00,
		0,
		'TXN_003_BANK_002',
		'Investment for VinFast VF8',
		1
	),
	(
		2,
		5,
		1100000.00,
		2,
		'TXN_004_DEBIT_001',
		'Co-investment for VinFast VF8',
		1
	);

-- Fund usage
INSERT INTO fund_usage (
		fund_id,
		usage_type_enum,
		amount,
		description,
		maintenance_cost_id
	)
VALUES (
		1,
		0,
		2500000.00,
		'Tesla Model 3 regular maintenance',
		1
	),
	(
		2,
		0,
		1800000.00,
		'VinFast VF8 battery service',
		2
	),
		(
		1,
		2,
		500000.00,
		'Charging station subscription',
		NULL
	),
	(
		2,
		3,
		300000.00,
		'Parking fees for October',
		NULL
	);

-- Fund usage votes
INSERT INTO fund_usage_votes (fund_usage_id, user_id, is_agree)
VALUES (1, 3, TRUE),                                 -- John agrees to Tesla maintenance
	(1, 4, TRUE),                                    -- Jane agrees to Tesla maintenance
	(2, 3, TRUE),                                    -- John agrees to VinFast service
	(2, 5, FALSE),                                   -- Mike disagrees with VinFast service cost
	(3, 3, TRUE),                                    -- John agrees to charging subscription
	(4, 5, TRUE);                                    -- Mike agrees to parking fees

-- Payments
INSERT INTO payments (
		user_id,
		amount,
		transaction_id,
		payment_gateway,
		status_enum,
		paid_at,
		fund_addition_id
	)
VALUES (
		3,
		800000.00,
		'PAY_001_VNPAY_001',
		'VNPay',
		1,
		NOW() - INTERVAL '20 days',
		1
	),
	(
		4,
		700000.00,
		'PAY_002_MOMO_001',
		'MoMo',
		1,
		NOW() - INTERVAL '18 days',
		2
	),
	(
		3,
		1200000.00,
		'PAY_003_VNPAY_002',
		'VNPay',
		1,
		NOW() - INTERVAL '10 days',
		3
	),
	(
		5,
		1100000.00,
		'PAY_004_ZALOPAY_001',
		'ZaloPay',
		1,
		NOW() - INTERVAL '8 days',
		4
	);

-- ==============================================================================
-- SAMPLE DATA - BOOKINGS & USAGE
-- ==============================================================================
-- Sample bookings, check-ins/outs, and usage records
-- ------------------------------------------------------------------------------

-- Bookings
INSERT INTO bookings (
		co_owner_id,
		vehicle_id,
		start_time,
		end_time,
		purpose,
		status_enum,
		approved_by,
		total_cost
	)
VALUES (
		3,
		1,
		NOW() + INTERVAL '1 day',
		NOW() + INTERVAL '1 day 4 hours',
		'Business meeting in District 1',
		1,
		2,
		150000.00
	),
	(
		4,
		1,
		NOW() + INTERVAL '3 days',
		NOW() + INTERVAL '3 days 6 hours',
		'Family trip to Vung Tau',
		0,
		NULL,
		300000.00
	),
	(
		5,
		2,
		NOW() + INTERVAL '2 days',
		NOW() + INTERVAL '2 days 8 hours',
		'Airport pickup for relatives',
		1,
		2,
		250000.00
	);

-- Check-ins
INSERT INTO check_ins (
		booking_id,
		staff_id,
		vehicle_station_id,
		vehicle_condition_id,
		check_time
	)
VALUES (1, 2, 1, 1, NOW() - INTERVAL '2 hours'),
	(3, 2, 2, 2, NOW() - INTERVAL '1 hour');

-- Check-outs
INSERT INTO check_outs (
		booking_id,
		staff_id,
		vehicle_station_id,
		vehicle_condition_id,
		check_time
	)
VALUES (1, 2, 1, 1, NOW() - INTERVAL '30 minutes'),
	(3, 2, 2, 2, NOW() - INTERVAL '15 minutes');

-- Vehicle usage records
INSERT INTO vehicle_usage_records (
		booking_id,
		vehicle_id,
		co_owner_id,
		check_in_id,
		check_out_id,
		start_time,
		end_time,
		duration_hours,
		distance_km,
		battery_used_percent,
		odometer_start,
		odometer_end,
		booking_cost,
		total_cost,
		purpose,
		was_qr_scanned,
		assisting_staff_id
	)
VALUES (
		1,
		1,
		3,
		1,
		1,
		NOW() - INTERVAL '2 hours',
		NOW() - INTERVAL '30 minutes',
		1.5,
		25,
		8,
		15000,
		15025,
		150000.00,
		150000.00,
		'Business meeting in District 1',
		FALSE,
		2
	),
	(
		3,
		2,
		5,
		2,
		2,
		NOW() - INTERVAL '1 hour',
		NOW() - INTERVAL '15 minutes',
		0.75,
		15,
		5,
		8500,
		8515,
		250000.00,
		250000.00,
		'Airport pickup for relatives',
		TRUE,
		2
	);

-- ==============================================================================
-- SAMPLE DATA - MAINTENANCE & UPGRADES
-- ==============================================================================
-- Sample maintenance costs and upgrade proposals
-- ------------------------------------------------------------------------------

-- Maintenance costs
INSERT INTO maintenance_costs (
		vehicle_id,
		booking_id,
		maintenance_type_enum,
		description,
		cost,
		is_paid,
		service_provider,
		service_date,
		odometer_reading
	)
VALUES (
		1,
		NULL,
		0,
		'Regular 6-month maintenance check',
		2500000.00,
		TRUE,
		'Tesla Service Center HCMC',
		'2024-10-15',
		14500
	),
	(
		2,
		NULL,
		1,
		'Battery diagnostic and software update',
		1800000.00,
		FALSE,
		'VinFast Service Center',
		'2024-10-28',
		8200
	);

-- Vehicle upgrade proposals
INSERT INTO vehicle_upgrade_proposals (
		vehicle_id,
		upgrade_type_enum,
		title,
		description,
		estimated_cost,
		justification,
		proposed_by_user_id,
		proposed_at
	)
VALUES (
		1,
		2,
		'Install Dash Camera System',
		'High-quality front and rear dash cameras for security',
		5000000.00,
		'Enhance security and insurance coverage',
		3,
		NOW() - INTERVAL '5 days'
	),
	(
		2,
		0,
		'Battery Capacity Upgrade',
		'Upgrade to higher capacity battery pack',
		25000000.00,
		'Increase range from 450km to 550km for longer trips',
		5,
		NOW() - INTERVAL '3 days'
	);

-- Vehicle upgrade votes
INSERT INTO vehicle_upgrade_votes (proposal_id, user_id, is_agree, comments)
VALUES (1, 3, TRUE, 'Great idea for security'),
	(1, 4, TRUE, 'Will help with insurance claims'),
	(2, 3, FALSE, 'Too expensive for now'),
	(
		2,
		5,
		TRUE,
		'Worth the investment for longer range'
	);

-- Add foreign key constraint for maintenance_costs after table creation
ALTER TABLE fund_usage
ADD CONSTRAINT fk_fund_usage_maintenance_cost
FOREIGN KEY (maintenance_cost_id) REFERENCES maintenance_costs(id);

-- ==============================================================================
-- SAMPLE DATA - NOTIFICATIONS & FILES
-- ==============================================================================
-- Sample notifications and file uploads
-- ------------------------------------------------------------------------------

-- Notification entities
INSERT INTO notification_entities (notification_type, additional_data_json)
VALUES (
		'BOOKING_APPROVED',
		'{"booking_id": 1, "vehicle_name": "EV Tesla Model 3", "start_time": "2024-11-02T10:00:00Z"}'
	),
	(
		'MAINTENANCE_SCHEDULED',
		'{"vehicle_id": 1, "maintenance_date": "2024-11-15", "service_provider": "Tesla Service Center"}'
	),
	(
		'PAYMENT_RECEIVED',
		'{"amount": 800000.00, "from_user": "John Doe", "fund_id": 1}'
	),
	(
		'UPGRADE_PROPOSAL_CREATED',
		'{"proposal_id": 1, "vehicle_name": "EV Tesla Model 3", "upgrade_title": "Install Dash Camera System"}'
	);

-- User notifications
INSERT INTO user_notifications (notification_id, user_id, read_at)
VALUES (1, 3, NULL),                                 -- John hasn't read booking approval yet
	(1, 4, NOW() - INTERVAL '1 hour'),              -- Jane read booking approval
	(2, 3, NULL),                                    -- John hasn't read maintenance notification
	(2, 4, NULL),                                    -- Jane hasn't read maintenance notification
	(3, 4, NOW() - INTERVAL '30 minutes'),          -- Jane read payment notification
	(4, 3, NOW() - INTERVAL '2 hours'),             -- John read upgrade proposal
	(4, 4, NULL);                                    -- Jane hasn't read upgrade proposal

-- File uploads (sample files)
INSERT INTO file_uploads (data, file_name, mime_type)
VALUES (
		decode('89504E470D0A1A0A', 'hex'),
		'tesla_model3_image.png',
		'image/png'
	),
	(
		decode('FFD8FFE0', 'hex'),
		'vinfast_vf8_photo.jpg',
		'image/jpeg'
	),
	(
		decode('25504446', 'hex'),
		'maintenance_receipt.pdf',
		'application/pdf'
	),
	(
		decode('504B0304', 'hex'),
		'vehicle_documents.zip',
		'application/zip'
	);

-- ==============================================================================
-- SAMPLE DATA - GROUPS
-- ==============================================================================
-- Sample groups, members, and group activities
-- ------------------------------------------------------------------------------

-- Groups
INSERT INTO groups (
        name,
        description,
        created_by,
        max_members,
        group_type_enum
    )
VALUES (
        'Tesla Co-ownership Group',
        'Group for sharing Tesla Model 3 ownership and costs',
        1,
        5,
        0
    ),
    (
        'VinFast Community',
        'Community group for VinFast VF8 co-owners',
        1,
        8,
        0
    );

-- Group members
INSERT INTO group_members (
        group_id,
        user_id,
        role_enum,
        investment_amount,
        ownership_percentage
    )
VALUES (1, 1, 2, 0, 0),                              -- Admin is Owner of group 1
    (1, 3, 0, 800000, 55),                           -- John - Member with 55% ownership
    (1, 4, 0, 700000, 45),                           -- Jane - Member with 45% ownership
    (2, 1, 2, 0, 0),                                 -- Admin is Owner of group 2
    (2, 3, 0, 1200000, 60),                          -- John - Member with 60% ownership
    (2, 5, 0, 1100000, 40);                          -- Mike - Member with 40% ownership

-- Group vehicles
INSERT INTO group_vehicles (group_id, vehicle_id)
VALUES (1, 1),                                       -- Tesla belongs to group 1
    (2, 2);                                          -- VinFast belongs to group 2

-- Group funds
INSERT INTO group_funds (
        group_id,
        fund_id,
        target_amount,
        current_amount,
        purpose
    )
VALUES (
        1,
        1,
        2000000,
        1500000,
        'Tesla Model 3 maintenance and operation fund'
    ),
    (
        2,
        2,
        3000000,
        2300000,
        'VinFast VF8 maintenance and upgrade fund'
    );

-- Group votes
INSERT INTO group_votes (
        group_id,
        title,
        description,
        vote_type_enum,
        created_by,
        end_time
    )
VALUES (
        1,
        'Approve Tesla Maintenance',
        'Vote to approve 2.5M VND maintenance cost for Tesla Model 3',
        0,
        1,
        NOW() + INTERVAL '7 days'
    ),
    (
        2,
        'VinFast Battery Upgrade',
        'Vote to upgrade VinFast battery capacity',
        2,
        1,
        NOW() + INTERVAL '10 days'
    );

-- Group vote responses
INSERT INTO group_vote_responses (vote_id, user_id, is_agree, comments)
VALUES (1, 3, TRUE, 'Necessary maintenance for safety'),
    (1, 4, TRUE, 'Agree with the cost estimate'),
    (2, 3, FALSE, 'Too expensive for now'),
    (2, 5, TRUE, 'Worth the investment for longer range');

-- Commit all changes
COMMIT;

-- ==============================================================================
-- END OF DATABASE SCHEMA
-- ==============================================================================
-- ==========================================================