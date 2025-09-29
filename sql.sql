
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;

-- Enum mappings converted to integer values with comments
-- user_role_enum: 1=co_owner, 2=staff, 3=admin
-- user_status_enum: 1=active, 2=inactive, 3=suspended
-- co_owner_status_enum: 1=active, 2=pending, 3=left
-- vehicle_status_enum: 1=available, 2=in_use, 3=maintenance, 4=unavailable
-- booking_status_enum: 1=pending, 2=confirmed, 3=active, 4=completed, 5=cancelled
-- condition_type_enum: 1=excellent, 2=good, 3=fair, 4=poor, 5=damaged
-- severity_type_enum: 1=low, 2=medium, 3=high, 4=critical
-- payment_method_enum: 1=bank_transfer, 2=credit_card, 3=debit_card, 4=cash
-- fund_addition_status_enum: 1=pending, 2=completed, 3=failed, 4=refunded
-- usage_type_enum: 1=maintenance, 2=insurance, 3=fuel, 4=parking, 5=other
-- payment_status_enum: 1=pending, 2=completed, 3=failed, 4=refunded
-- maintenance_type_enum: 1=routine, 2=repair, 3=emergency, 4=upgrade
-- service_type_enum: 1=maintenance, 2=repair, 3=cleaning, 4=inspection, 5=upgrade
-- service_status_enum: 1=active, 2=inactive
-- service_usage_status_enum: 1=scheduled, 2=in_progress, 3=completed, 4=cancelled

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
	password_hash VARCHAR(255) NOT NULL,
	first_name VARCHAR(100) NOT NULL,
	last_name VARCHAR(100) NOT NULL,
	phone VARCHAR(20),
	date_of_birth DATE,
	address TEXT,
	profile_image_url VARCHAR(500),
	status_enum INTEGER DEFAULT 1, -- user_status_enum: 1=active, 2=inactive, 3=suspended
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE roles (
	id SERIAL PRIMARY KEY,
	role_name_enum INTEGER UNIQUE NOT NULL -- user_role_enum: 1=co_owner, 2=staff, 3=admin
);

CREATE TABLE user_roles (
    user_id INTEGER REFERENCES users(id),
    role_id INTEGER REFERENCES roles(id),
    PRIMARY KEY (user_id, role_id)
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

CREATE TABLE groups (
	id SERIAL PRIMARY KEY,
	name VARCHAR(200) NOT NULL,
	description TEXT,
	created_by INTEGER REFERENCES users(id),
	fund_id INTEGER REFERENCES funds(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE co_owners (
	user_id INTEGER PRIMARY KEY REFERENCES users(id),
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE co_owner_groups (
    co_owner_id INTEGER REFERENCES co_owners(user_id),
    group_id INTEGER REFERENCES groups(id),
    PRIMARY KEY (co_owner_id, group_id),
    ownership_percentage DECIMAL(5,2) NOT NULL,
    join_date DATE NOT NULL,
    investment_amount DECIMAL(15,2) NOT NULL,
    status_enum INTEGER DEFAULT 2 -- co_owner_status_enum: 1=active, 2=pending, 3=left
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
	group_id INTEGER REFERENCES groups(id),
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
	status_enum INTEGER DEFAULT 1, -- vehicle_status_enum: 1=available, 2=in_use, 3=maintenance, 4=unavailable
	location_latitude DECIMAL(10,8),
	location_longitude DECIMAL(11,8),
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
	condition_type_enum INTEGER, -- condition_type_enum: 1=excellent, 2=good, 3=fair, 4=poor, 5=damaged
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
	status_enum INTEGER DEFAULT 1, -- booking_status_enum: 1=pending, 2=confirmed, 3=active, 4=completed, 5=cancelled
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
	payment_method_enum INTEGER, -- payment_method_enum: 1=bank_transfer, 2=credit_card, 3=debit_card, 4=cash
	transaction_id VARCHAR(100),
	description TEXT,
	status_enum INTEGER DEFAULT 1, -- fund_addition_status_enum: 1=pending, 2=completed, 3=failed, 4=refunded
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE maintenance_costs (
	id SERIAL PRIMARY KEY,
	vehicle_id INTEGER REFERENCES vehicles(id),
	booking_id INTEGER REFERENCES bookings(id),
	maintenance_type_enum INTEGER, -- maintenance_type_enum: 1=routine, 2=repair, 3=emergency, 4=upgrade
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
	usage_type_enum INTEGER, -- usage_type_enum: 1=maintenance, 2=insurance, 3=fuel, 4=parking, 5=other
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
	status_enum INTEGER DEFAULT 1, -- payment_status_enum: 1=pending, 2=completed, 3=failed, 4=refunded
	paid_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	fund_addition_id INTEGER REFERENCES fund_additions(id)
);

-- ================================================
-- INDEXES FOR PERFORMANCE OPTIMIZATION
-- ================================================

-- Foreign Key Indexes (improve JOIN performance)
CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);
CREATE INDEX idx_groups_created_by ON groups(created_by);
CREATE INDEX idx_groups_fund_id ON groups(fund_id);
CREATE INDEX idx_co_owner_groups_co_owner_id ON co_owner_groups(co_owner_id);
CREATE INDEX idx_co_owner_groups_group_id ON co_owner_groups(group_id);
CREATE INDEX idx_driving_licenses_co_owner_id ON driving_licenses(co_owner_id);
CREATE INDEX idx_vehicles_group_id ON vehicles(group_id);
CREATE INDEX idx_vehicle_conditions_vehicle_id ON vehicle_conditions(vehicle_id);
CREATE INDEX idx_vehicle_conditions_reported_by ON vehicle_conditions(reported_by);
CREATE INDEX idx_bookings_co_owner_id ON bookings(co_owner_id);
CREATE INDEX idx_bookings_vehicle_id ON bookings(vehicle_id);
CREATE INDEX idx_bookings_approved_by ON bookings(approved_by);
CREATE INDEX idx_check_ins_booking_id ON check_ins(booking_id);
CREATE INDEX idx_check_ins_staff_id ON check_ins(staff_id);
CREATE INDEX idx_check_ins_vehicle_station_id ON check_ins(vehicle_station_id);
CREATE INDEX idx_check_ins_vehicle_condition_id ON check_ins(vehicle_condition_id);
CREATE INDEX idx_check_outs_booking_id ON check_outs(booking_id);
CREATE INDEX idx_check_outs_staff_id ON check_outs(staff_id);
CREATE INDEX idx_check_outs_vehicle_station_id ON check_outs(vehicle_station_id);
CREATE INDEX idx_check_outs_vehicle_condition_id ON check_outs(vehicle_condition_id);
CREATE INDEX idx_fund_additions_fund_id ON fund_additions(fund_id);
CREATE INDEX idx_fund_additions_co_owner_id ON fund_additions(co_owner_id);
CREATE INDEX idx_maintenance_costs_vehicle_id ON maintenance_costs(vehicle_id);
CREATE INDEX idx_maintenance_costs_booking_id ON maintenance_costs(booking_id);
CREATE INDEX idx_fund_usage_fund_id ON fund_usage(fund_id);
CREATE INDEX idx_fund_usage_maintenance_cost_id ON fund_usage(maintenance_cost_id);
CREATE INDEX idx_fund_usage_votes_fund_usage_id ON fund_usage_votes(fund_usage_id);
CREATE INDEX idx_fund_usage_votes_user_id ON fund_usage_votes(user_id);
CREATE INDEX idx_payments_user_id ON payments(user_id);
CREATE INDEX idx_payments_fund_addition_id ON payments(fund_addition_id);

-- Status and Search Indexes (improve WHERE clause performance)
CREATE INDEX idx_users_status_enum ON users(status_enum);
CREATE INDEX idx_users_email ON users(email); -- Already UNIQUE but explicit for searches
CREATE INDEX idx_users_phone ON users(phone);
CREATE INDEX idx_co_owner_groups_status_enum ON co_owner_groups(status_enum);
CREATE INDEX idx_vehicles_status_enum ON vehicles(status_enum);
CREATE INDEX idx_vehicles_license_plate ON vehicles(license_plate); -- Already UNIQUE but for searches
CREATE INDEX idx_vehicles_vin ON vehicles(vin); -- Already UNIQUE but for searches
CREATE INDEX idx_bookings_status_enum ON bookings(status_enum);
CREATE INDEX idx_fund_additions_status_enum ON fund_additions(status_enum);
CREATE INDEX idx_maintenance_costs_is_paid ON maintenance_costs(is_paid);
CREATE INDEX idx_payments_status_enum ON payments(status_enum);
CREATE INDEX idx_vehicle_conditions_condition_type_enum ON vehicle_conditions(condition_type_enum);
CREATE INDEX idx_vehicle_conditions_damage_reported ON vehicle_conditions(damage_reported);

-- Date-based Indexes (improve date range queries)
CREATE INDEX idx_users_created_at ON users(created_at);
CREATE INDEX idx_co_owner_groups_join_date ON co_owner_groups(join_date);
CREATE INDEX idx_driving_licenses_expiry_date ON driving_licenses(expiry_date);
CREATE INDEX idx_vehicles_purchase_date ON vehicles(purchase_date);
CREATE INDEX idx_vehicles_warranty_until ON vehicles(warranty_until);
CREATE INDEX idx_bookings_start_time ON bookings(start_time);
CREATE INDEX idx_bookings_end_time ON bookings(end_time);
CREATE INDEX idx_check_ins_check_time ON check_ins(check_time);
CREATE INDEX idx_check_outs_check_time ON check_outs(check_time);
CREATE INDEX idx_maintenance_costs_service_date ON maintenance_costs(service_date);
CREATE INDEX idx_payments_paid_at ON payments(paid_at);

-- Composite Indexes (optimize common multi-column queries)
CREATE INDEX idx_bookings_vehicle_status_enum_time ON bookings(vehicle_id, status_enum, start_time);
CREATE INDEX idx_bookings_co_owner_status_enum_time ON bookings(co_owner_id, status_enum, start_time);
CREATE INDEX idx_vehicle_conditions_vehicle_damage ON vehicle_conditions(vehicle_id, damage_reported, created_at);
CREATE INDEX idx_fund_usage_votes_composite ON fund_usage_votes(fund_usage_id, is_agree);
CREATE INDEX idx_maintenance_costs_vehicle_paid ON maintenance_costs(vehicle_id, is_paid, service_date);
CREATE INDEX idx_fund_additions_co_owner_status_enum ON fund_additions(co_owner_id, status_enum, created_at);
CREATE INDEX idx_payments_user_status_enum ON payments(user_id, status_enum, created_at);
CREATE INDEX idx_co_owner_groups_group_status_enum ON co_owner_groups(group_id, status_enum);
CREATE INDEX idx_users_status_enum_created ON users(status_enum, created_at);

-- Text Search Indexes (for LIKE queries)
CREATE INDEX idx_users_name_search ON users USING gin(to_tsvector('english', first_name || ' ' || last_name));
CREATE INDEX idx_vehicles_brand_model ON vehicles USING gin(to_tsvector('english', brand || ' ' || model));
CREATE INDEX idx_vehicle_stations_name_search ON vehicle_stations USING gin(to_tsvector('english', name));
CREATE INDEX idx_groups_name_search ON groups USING gin(to_tsvector('english', name));

-- Geospatial Indexes (for location-based queries)
CREATE INDEX idx_vehicles_location ON vehicles(location_latitude, location_longitude);
CREATE INDEX idx_vehicle_stations_location ON vehicle_stations(location_latitude, location_longitude);

-- Performance Indexes for Aggregations
CREATE INDEX idx_fund_usage_fund_amount ON fund_usage(fund_id, amount, created_at);
CREATE INDEX idx_maintenance_costs_cost_date ON maintenance_costs(cost, service_date);
CREATE INDEX idx_payments_amount_date ON payments(amount, paid_at);

-- Unique Constraint Indexes (already created by UNIQUE, but listed for completeness)
-- CREATE UNIQUE INDEX idx_users_email_unique ON users(email); -- Already exists
-- CREATE UNIQUE INDEX idx_vehicles_vin_unique ON vehicles(vin); -- Already exists  
-- CREATE UNIQUE INDEX idx_vehicles_license_plate_unique ON vehicles(license_plate); -- Already exists
-- CREATE UNIQUE INDEX idx_driving_licenses_number_unique ON driving_licenses(license_number); -- Already exists
-- CREATE UNIQUE INDEX idx_payments_transaction_id_unique ON payments(transaction_id); -- Already exists
