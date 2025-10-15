
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;

-- Enum mappings converted to integer values with comments
-- All enums use 0-based indexing for consistency

-- USER RELATED ENUMS
-- user_role_enum: 0=co_owner, 1=staff, 2=admin
-- user_status_enum: 0=active, 1=inactive, 2=suspended

-- VEHICLE RELATED ENUMS  
-- vehicle_status_enum: 0=available, 1=in_use, 2=maintenance, 3=unavailable
-- vehicle_verification_status_enum: 0=pending, 1=VerificationRequested, 2=requires_recheck, 3=verified, 4=rejected
-- condition_type_enum: 0=excellent, 1=good, 2=fair, 3=poor, 4=damaged

-- CONTRACT RELATED ENUMS
-- contract_status_enum: 0=pending, 1=active, 2=rejected

-- BOOKING RELATED ENUMS
-- booking_status_enum: 0=pending, 1=confirmed, 2=active, 3=completed, 4=cancelled

-- PAYMENT & FUND RELATED ENUMS
-- payment_method_enum: 0=bank_transfer, 1=credit_card, 2=debit_card, 3=cash
-- fund_addition_status_enum: 0=pending, 1=completed, 2=failed, 3=refunded
-- payment_status_enum: 0=pending, 1=completed, 2=failed, 3=refunded

-- MAINTENANCE & USAGE RELATED ENUMS
-- maintenance_type_enum: 0=routine, 1=repair, 2=emergency, 3=upgrade
-- usage_type_enum: 0=maintenance, 1=insurance, 2=fuel, 3=parking, 4=other

-- Create tables

CREATE TABLE configurations (
	key VARCHAR(100) PRIMARY KEY,
	value TEXT NOT NULL,
	description TEXT,
	updated_at TIMESTAMP DEFAULT NOW()
);

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
	role_enum INTEGER DEFAULT 0, -- user_role_enum: 0=co_owner, 1=staff, 2=admin
	status_enum INTEGER DEFAULT 0, -- user_status_enum: 0=active, 1=inactive, 2=suspended
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE user_refresh_tokens (
	user_id INTEGER PRIMARY KEY REFERENCES users(id),
	refresh_token VARCHAR(255) NOT NULL,
	expires_at TIMESTAMP NOT NULL
);

CREATE TABLE funds (
	id SERIAL PRIMARY KEY,
	current_balance DECIMAL(15,2) DEFAULT 0,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE co_owners (
	user_id INTEGER PRIMARY KEY REFERENCES users(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE driving_licenses (
    id SERIAL PRIMARY KEY,
    co_owner_id INTEGER REFERENCES co_owners(user_id),
    license_number VARCHAR(50) UNIQUE NOT NULL,
    issued_by VARCHAR(100) NOT NULL,
    issue_date DATE NOT NULL,
    expiry_date DATE,
    license_image_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

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
	battery_capacity DECIMAL(6,2),
	range_km INTEGER,
	purchase_date DATE NOT NULL,
	purchase_price DECIMAL(15,2) NOT NULL,
    warranty_until DATE,
	distance_travelled INTEGER DEFAULT 0,
	status_enum INTEGER DEFAULT 0, -- vehicle_status_enum: 0=available, 1=in_use, 2=maintenance, 3=unavailable
	verification_status_enum INTEGER DEFAULT 0, -- vehicle_verification_status_enum: 0=pending, 1=verified, 2=rejected, 3=requires_recheck
	location_latitude DECIMAL(10,8),
	location_longitude DECIMAL(11,8),
	created_by INTEGER REFERENCES users(id),
	fund_id INTEGER REFERENCES funds(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE vehicle_verification_history (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	staff_id INTEGER REFERENCES users(id), 
	status_enum INTEGER NOT NULL,
	notes TEXT, -- Detailed notes from the verification process
	images JSONB, -- Array of image URLs from verification process
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE vehicle_co_owners (
    co_owner_id INTEGER REFERENCES co_owners(user_id),
    vehicle_id INTEGER REFERENCES vehicles(id),
    PRIMARY KEY (co_owner_id, vehicle_id),
    ownership_percentage DECIMAL(5,2) NOT NULL,
    investment_amount DECIMAL(15,2) NOT NULL,
    status_enum INTEGER DEFAULT 0, -- contract_status_enum: 0=active, 1=pending, 2=terminated
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE vehicle_stations (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    address TEXT NOT NULL,
    contact_number VARCHAR(20),
    location_latitude DECIMAL(10,8) NOT NULL,
    location_longitude DECIMAL(11,8) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE vehicle_conditions (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	reported_by INTEGER REFERENCES users(id),
	condition_type_enum INTEGER, -- condition_type_enum: 0=excellent, 1=good, 2=fair, 3=poor, 4=damaged
	description TEXT,
	photo_urls TEXT,
	odometer_reading INTEGER,
	fuel_level DECIMAL(5,2),
	damage_reported BOOLEAN DEFAULT FALSE,
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE bookings (
	id SERIAL PRIMARY KEY,
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	vehicle_id INTEGER REFERENCES vehicles(id),
	start_time TIMESTAMP NOT NULL,
	end_time TIMESTAMP NOT NULL,
	purpose VARCHAR(500),
	status_enum INTEGER DEFAULT 0, -- booking_status_enum: 0=pending, 1=confirmed, 2=active, 3=completed, 4=cancelled
	approved_by INTEGER REFERENCES users(id),
	total_cost DECIMAL(10,2),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

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

CREATE TABLE fund_additions (
	id SERIAL PRIMARY KEY,
	fund_id INTEGER REFERENCES funds(id),
	co_owner_id INTEGER REFERENCES co_owners(user_id),
	amount DECIMAL(15,2) NOT NULL,
	payment_method_enum INTEGER, -- payment_method_enum: 0=bank_transfer, 1=credit_card, 2=debit_card, 3=cash
	transaction_id VARCHAR(100),
	description TEXT,
	status_enum INTEGER DEFAULT 0, -- fund_addition_status_enum: 0=pending, 1=completed, 2=failed, 3=refunded
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE maintenance_costs (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	booking_id INTEGER REFERENCES bookings(id),
	maintenance_type_enum INTEGER, -- maintenance_type_enum: 0=routine, 1=repair, 2=emergency, 3=upgrade
	description TEXT NOT NULL,
	cost DECIMAL(10,2) NOT NULL,
    is_paid BOOLEAN DEFAULT FALSE,
	service_provider VARCHAR(200),
	service_date DATE NOT NULL,
	odometer_reading INTEGER,
	image_url VARCHAR(500),
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE fund_usage (
	id SERIAL PRIMARY KEY,
	fund_id INTEGER REFERENCES funds(id),
	usage_type_enum INTEGER, -- usage_type_enum: 0=maintenance, 1=insurance, 2=fuel, 3=parking, 4=other
	amount DECIMAL(15,2) NOT NULL,
	description TEXT NOT NULL,
	image_url VARCHAR(500),
	maintenance_cost_id INTEGER REFERENCES maintenance_costs(id),
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE fund_usage_votes (
	fund_usage_id INTEGER REFERENCES fund_usage(id) NOT NULL,
	user_id INTEGER REFERENCES users(id) NOT NULL,
	is_agree BOOLEAN NOT NULL,
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE payments (
	id SERIAL PRIMARY KEY,
	user_id INTEGER REFERENCES users(id),
	amount DECIMAL(10,2) NOT NULL,
	transaction_id VARCHAR(100) UNIQUE,
	payment_gateway VARCHAR(50),
	status_enum INTEGER DEFAULT 0, -- payment_status_enum: 0=pending, 1=completed, 2=failed, 3=refunded
	paid_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	fund_addition_id INTEGER REFERENCES fund_additions(id)
);

CREATE TABLE file_uploads (
    id SERIAL PRIMARY KEY,
    data BYTEA NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    mime_type VARCHAR(100) NOT NULL,
    uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
