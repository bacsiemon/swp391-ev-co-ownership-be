-- EV Co-Ownership Database Schema
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
-- CORE SYSTEM TABLES
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
-- ==========================================================
-- FUND AND FINANCIAL TABLES
-- ==========================================================
-- Vehicle fund management
CREATE TABLE funds (
	id SERIAL PRIMARY KEY,
	current_balance DECIMAL(15, 2) DEFAULT 0,
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
	status_enum INTEGER DEFAULT 0,
	-- payment_status_enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
	paid_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	fund_addition_id INTEGER REFERENCES fund_additions(id)
);
-- ==========================================================
-- VEHICLE MANAGEMENT TABLES
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
-- ==========================================================
-- BOOKING AND USAGE TABLES  
-- ==========================================================
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
-- Vehicle usage tracking (NEW - for complete trip records)
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
-- ==========================================================
-- MAINTENANCE AND UPGRADE TABLES
-- ==========================================================
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
-- Vehicle upgrade proposals (NEW)
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
-- Vehicle upgrade voting (NEW)
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
-- REMOVE DEPRECATED/UNUSED TABLES
-- These tables are not used in the current codebase:
-- - groups, group_members, votes, vote_options, vote_results 
-- - vehicle_verification_history (commented out in models)
-- ==========================================================
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
-- END OF SCHEMA
-- ==========================================================