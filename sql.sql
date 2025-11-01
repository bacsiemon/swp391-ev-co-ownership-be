-- EV Co-Ownership Database Schema - FIXED VERSION
-- Compatible with PostgreSQL and Entity Framework Core
-- Last updated: 2025-11-01
-- ==========================================================
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
-- Enum mappings converted to integer values with comments
-- All enums use 0-based indexing for consistency
-- USER RELATED ENUMS
-- user_role_enum: 0=CoOwner, 1=Staff, 2=Admin
-- user_status_enum: 0=Active, 1=Inactive, 2=Suspended
-- VEHICLE RELATED ENUMS  
-- vehicle_status_enum: 0=Available, 1=InUse, 2=Maintenance, 3=Unavailable
-- vehicle_verification_status_enum: 0=Pending, 1=VerificationRequested, 2=RequiresRecheck, 3=Verified, 4=Rejected
-- condition_type_enum: 0=Excellent, 1=Good, 2=Fair, 3=Poor, 4=Damaged
-- BOOKING RELATED ENUMS
-- booking_status_enum: 0=Pending, 1=Confirmed, 2=Active, 3=Completed, 4=Cancelled
-- PAYMENT & FUND RELATED ENUMS
-- payment_method_enum: 0=BankTransfer, 1=CreditCard, 2=DebitCard, 3=Cash
-- fund_addition_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
-- payment_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
-- MAINTENANCE & USAGE RELATED ENUMS
-- maintenance_type_enum: 0=Routine, 1=Repair, 2=Emergency, 3=Upgrade
-- usage_type_enum: 0=Maintenance, 1=Insurance, 2=Fuel, 3=Parking, 4=Other
-- DRIVING LICENSE VERIFICATION ENUMS
-- driving_license_verification_status_enum: 0=Pending, 1=Verified, 2=Rejected, 3=Expired
-- UPGRADE RELATED ENUMS
-- upgrade_type_enum: 0=BatteryUpgrade, 1=SoftwareUpdate, 2=HardwareInstallation, 3=Maintenance, 4=Cosmetic
-- ==========================================================
-- CORE SYSTEM TABLES (No dependencies)
-- ==========================================================
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
	role_enum INTEGER DEFAULT 0,
	-- user_role_enum: 0=CoOwner, 1=Staff, 2=Admin
	status_enum INTEGER DEFAULT 0,
	-- user_status_enum: 0=Active, 1=Inactive, 2=Suspended
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
-- ==========================================================
-- DEPENDENT TABLES (Level 1)
-- ==========================================================
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
	status_enum INTEGER DEFAULT 0,
	-- vehicle_status_enum: 0=Available, 1=InUse, 2=Maintenance, 3=Unavailable
	verification_status_enum INTEGER DEFAULT 0,
	-- vehicle_verification_status_enum: 0=Pending, 1=VerificationRequested, 2=RequiresRecheck, 3=Verified, 4=Rejected
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
	verification_status INTEGER DEFAULT 0,
	-- driving_license_verification_status_enum: 0=Pending, 1=Verified, 2=Rejected, 3=Expired
	reject_reason TEXT,
	verified_by_user_id INTEGER REFERENCES users(id),
	verified_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);
-- Fund additions (money coming in)
CREATE TABLE fund_additions (
	id SERIAL PRIMARY KEY,
	fund_id INTEGER REFERENCES funds(id),
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	amount DECIMAL(15, 2) NOT NULL,
	payment_method_enum INTEGER,
	-- payment_method_enum: 0=BankTransfer, 1=CreditCard, 2=DebitCard, 3=Cash
	transaction_id VARCHAR(100),
	description TEXT,
	status_enum INTEGER DEFAULT 0,
	-- fund_addition_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
	created_at TIMESTAMP DEFAULT NOW()
);
-- Vehicle co-ownership relationships
CREATE TABLE vehicle_co_owners (
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	vehicle_id INTEGER REFERENCES vehicles(id),
	PRIMARY KEY (co_owner_id, vehicle_id),
	ownership_percentage DECIMAL(5, 2) NOT NULL,
	investment_amount DECIMAL(15, 2) NOT NULL,
	status_enum INTEGER DEFAULT 0,
	-- 0=Active, 1=Pending, 2=Terminated
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);
-- Vehicle condition reports
CREATE TABLE vehicle_conditions (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	reported_by INTEGER REFERENCES users(id),
	condition_type_enum INTEGER,
	-- condition_type_enum: 0=Excellent, 1=Good, 2=Fair, 3=Poor, 4=Damaged
	description TEXT,
	photo_urls TEXT,
	odometer_reading INTEGER,
	fuel_level DECIMAL(5, 2),
	damage_reported BOOLEAN DEFAULT FALSE,
	created_at TIMESTAMP DEFAULT NOW()
);
-- Vehicle bookings
CREATE TABLE bookings (
	id SERIAL PRIMARY KEY,
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	vehicle_id INTEGER REFERENCES vehicles(id),
	start_time TIMESTAMP NOT NULL,
	end_time TIMESTAMP NOT NULL,
	purpose VARCHAR(500),
	status_enum INTEGER DEFAULT 0,
	-- booking_status_enum: 0=Pending, 1=Confirmed, 2=Active, 3=Completed, 4=Cancelled
	approved_by INTEGER REFERENCES users(id),
	total_cost DECIMAL(10, 2),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);
-- ==========================================================
-- DEPENDENT TABLES (Level 2)
-- ==========================================================
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
-- Maintenance costs
CREATE TABLE maintenance_costs (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	booking_id INTEGER REFERENCES bookings(id),
	maintenance_type_enum INTEGER,
	-- maintenance_type_enum: 0=Routine, 1=Repair, 2=Emergency, 3=Upgrade
	description TEXT NOT NULL,
	cost DECIMAL(10, 2) NOT NULL,
	is_paid BOOLEAN DEFAULT FALSE,
	service_provider VARCHAR(200),
	service_date DATE NOT NULL,
	odometer_reading INTEGER,
	image_url VARCHAR(500),
	created_at TIMESTAMP DEFAULT NOW()
);
-- Fund usage (money going out)
CREATE TABLE fund_usage (
	id SERIAL PRIMARY KEY,
	fund_id INTEGER REFERENCES funds(id),
	usage_type_enum INTEGER,
	-- usage_type_enum: 0=Maintenance, 1=Insurance, 2=Fuel, 3=Parking, 4=Other
	amount DECIMAL(15, 2) NOT NULL,
	description TEXT NOT NULL,
	image_url VARCHAR(500),
	maintenance_cost_id INTEGER REFERENCES maintenance_costs(id),
	created_at TIMESTAMP DEFAULT NOW()
);
-- Payment processing
CREATE TABLE payments (
	id SERIAL PRIMARY KEY,
	user_id INTEGER REFERENCES users(id),
	amount DECIMAL(10, 2) NOT NULL,
	transaction_id VARCHAR(100) UNIQUE,
	payment_gateway VARCHAR(50),
	status_enum INTEGER DEFAULT 0,
	-- payment_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
	paid_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	fund_addition_id INTEGER REFERENCES fund_additions(id)
);
-- ==========================================================
-- DEPENDENT TABLES (Level 3)
-- ==========================================================
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
-- Fund usage voting
CREATE TABLE fund_usage_votes (
	fund_usage_id INTEGER REFERENCES fund_usage(id) NOT NULL,
	user_id INTEGER REFERENCES users(id) NOT NULL,
	is_agree BOOLEAN NOT NULL,
	created_at TIMESTAMP DEFAULT NOW(),
	PRIMARY KEY (fund_usage_id, user_id)
);
-- Vehicle upgrade proposals
CREATE TABLE vehicle_upgrade_proposals (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	upgrade_type_enum INTEGER NOT NULL,
	-- upgrade_type_enum: 0=BatteryUpgrade, 1=SoftwareUpdate, 2=HardwareInstallation, 3=Maintenance, 4=Cosmetic
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
	status VARCHAR(20) DEFAULT 'Pending',
	-- Pending, Approved, Rejected, Cancelled
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
-- ==========================================================
-- NOTIFICATION SYSTEM TABLES
-- ==========================================================
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
-- ==========================================================
-- FILE MANAGEMENT TABLE
-- ==========================================================
-- File uploads
CREATE TABLE file_uploads (
	id SERIAL PRIMARY KEY,
	data BYTEA NOT NULL,
	file_name VARCHAR(255) NOT NULL,
	mime_type VARCHAR(100) NOT NULL,
	uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
-- ==========================================================
-- INDEXES FOR PERFORMANCE OPTIMIZATION
-- ==========================================================
-- User-related indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_normalized_email ON users(normalized_email);
CREATE INDEX idx_users_role_status ON users(role_enum, status_enum);
-- Vehicle-related indexes
CREATE INDEX idx_vehicles_status ON vehicles(status_enum);
CREATE INDEX idx_vehicles_verification_status ON vehicles(verification_status_enum);
CREATE INDEX idx_vehicles_fund_id ON vehicles(fund_id);
CREATE INDEX idx_vehicles_created_by ON vehicles(created_by);
-- Booking-related indexes
CREATE INDEX idx_bookings_co_owner_id ON bookings(co_owner_id);
CREATE INDEX idx_bookings_vehicle_id ON bookings(vehicle_id);
CREATE INDEX idx_bookings_status ON bookings(status_enum);
CREATE INDEX idx_bookings_start_time ON bookings(start_time);
CREATE INDEX idx_bookings_end_time ON bookings(end_time);
-- License-related indexes
CREATE INDEX idx_driving_licenses_co_owner_id ON driving_licenses(co_owner_id);
CREATE INDEX idx_driving_licenses_verification_status ON driving_licenses(verification_status);
CREATE INDEX idx_driving_licenses_license_number ON driving_licenses(license_number);
-- Fund-related indexes
CREATE INDEX idx_fund_additions_fund_id ON fund_additions(fund_id);
CREATE INDEX idx_fund_additions_co_owner_id ON fund_additions(co_owner_id);
CREATE INDEX idx_fund_usage_fund_id ON fund_usage(fund_id);
-- Usage records indexes
CREATE INDEX idx_vehicle_usage_records_vehicle_id ON vehicle_usage_records(vehicle_id);
CREATE INDEX idx_vehicle_usage_records_co_owner_id ON vehicle_usage_records(co_owner_id);
CREATE INDEX idx_vehicle_usage_records_booking_id ON vehicle_usage_records(booking_id);
CREATE INDEX idx_vehicle_usage_records_start_time ON vehicle_usage_records(start_time);
-- Check-in/out indexes
CREATE INDEX idx_check_ins_booking_id ON check_ins(booking_id);
CREATE INDEX idx_check_outs_booking_id ON check_outs(booking_id);
-- Notification indexes
CREATE INDEX idx_user_notifications_user_id ON user_notifications(user_id);
CREATE INDEX idx_user_notifications_read_at ON user_notifications(read_at);
-- ==========================================================
-- SAMPLE DATA FOR TESTING (Optional)
-- ==========================================================
-- Insert default admin user (password: Admin123!)
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
		-- Admin123!
		'$2a$11$8UE1WQz8Ql.Ua/0TK2zV1O',
		'System',
		'Administrator',
		2,
		-- Admin
		0 -- Active
	);
-- Insert default staff user (password: Staff123!)
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
		-- Staff123!
		'$2a$11$9VF2XRz9Rm.Vb/1UL3aW2P',
		'System',
		'Staff',
		1,
		-- Staff
		0 -- Active
	);
-- Insert default configuration values
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
-- ==========================================================
-- SAMPLE DATA FOR ALL TABLES
-- ==========================================================
-- Insert sample co-owners (linking to existing users)
INSERT INTO co_owners (user_id)
VALUES (1),
	(2);
-- Admin and Staff will also be co-owners for testing
-- Insert additional test users
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
-- Insert additional co-owners for new users
INSERT INTO co_owners (user_id)
VALUES (3),
	(4),
	(5);
-- Insert user refresh tokens
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
-- Insert sample funds
INSERT INTO funds (current_balance)
VALUES (1500000.00),
	-- Fund 1: 1.5M VND
	(2300000.00);
-- Fund 2: 2.3M VND
-- Insert vehicle stations
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
-- Insert sample vehicles
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
-- Insert driving licenses
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
-- Insert fund additions
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
-- Insert vehicle co-owners
INSERT INTO vehicle_co_owners (
		co_owner_id,
		vehicle_id,
		ownership_percentage,
		investment_amount,
		status_enum
	)
VALUES (3, 1, 55.00, 800000.00, 0),
	-- John owns 55% of Tesla
	(4, 1, 45.00, 700000.00, 0),
	-- Jane owns 45% of Tesla
	(3, 2, 60.00, 1200000.00, 0),
	-- John owns 60% of VinFast
	(5, 2, 40.00, 1100000.00, 0);
-- Mike owns 40% of VinFast
-- Insert vehicle conditions
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
-- Insert bookings
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
-- Insert check-ins
INSERT INTO check_ins (
		booking_id,
		staff_id,
		vehicle_station_id,
		vehicle_condition_id,
		check_time
	)
VALUES (1, 2, 1, 1, NOW() - INTERVAL '2 hours'),
	(3, 2, 2, 2, NOW() - INTERVAL '1 hour');
-- Insert check-outs
INSERT INTO check_outs (
		booking_id,
		staff_id,
		vehicle_station_id,
		vehicle_condition_id,
		check_time
	)
VALUES (1, 2, 1, 1, NOW() - INTERVAL '30 minutes'),
	(3, 2, 2, 2, NOW() - INTERVAL '15 minutes');
-- Insert maintenance costs
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
-- Insert fund usage
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
-- Insert vehicle usage records
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
-- Insert fund usage votes
INSERT INTO fund_usage_votes (fund_usage_id, user_id, is_agree)
VALUES (1, 3, TRUE),
	-- John agrees to Tesla maintenance
	(1, 4, TRUE),
	-- Jane agrees to Tesla maintenance
	(2, 3, TRUE),
	-- John agrees to VinFast service
	(2, 5, FALSE),
	-- Mike disagrees with VinFast service cost
	(3, 3, TRUE),
	-- John agrees to charging subscription
	(4, 5, TRUE);
-- Mike agrees to parking fees
-- Insert vehicle upgrade proposals
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
-- Insert vehicle upgrade votes
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
-- Insert payments
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
-- Insert notification entities
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
-- Insert user notifications
INSERT INTO user_notifications (notification_id, user_id, read_at)
VALUES (1, 3, NULL),
	-- John hasn't read booking approval yet
	(1, 4, NOW() - INTERVAL '1 hour'),
	-- Jane read booking approval
	(2, 3, NULL),
	-- John hasn't read maintenance notification
	(2, 4, NULL),
	-- Jane hasn't read maintenance notification
	(3, 4, NOW() - INTERVAL '30 minutes'),
	-- Jane read payment notification
	(4, 3, NOW() - INTERVAL '2 hours'),
	-- John read upgrade proposal
	(4, 4, NULL);
-- Jane hasn't read upgrade proposal
-- Insert file uploads (sample files)
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
-- ==========================================================
-- END OF SCHEMA
-- ==========================================================