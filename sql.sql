
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
	images_json TEXT, -- Array of image URLs from verification process
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


CREATE TABLE notification_entities (
	id SERIAL PRIMARY KEY,
	notification_type TEXT NOT NULL,
	additional_data_json TEXT, 
	created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE user_notifications (
	id SERIAL PRIMARY KEY,
	notification_id INTEGER REFERENCES notification_entities(id) ON DELETE CASCADE,
	user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
	read_at TIMESTAMP,
	UNIQUE(notification_id, user_id)
);

-- ALTER scripts to change JSONB fields to TEXT with _json suffix
-- Run these scripts if tables already exist

-- Alter vehicle_verification_history table
ALTER TABLE vehicle_verification_history 
	RENAME COLUMN images TO images_json;

ALTER TABLE vehicle_verification_history 
	ALTER COLUMN images_json TYPE TEXT USING images_json::TEXT;

-- Alter notification_entities table
ALTER TABLE notification_entities 
	RENAME COLUMN additional_data TO additional_data_json;

ALTER TABLE notification_entities 
	ALTER COLUMN additional_data_json TYPE TEXT USING additional_data_json::TEXT;

-- =============================================================================
-- MANAGER ACCOUNT MANAGEMENT
-- =============================================================================
-- Views, Indexes, and Functions for Manager Account operations

-- Create index for faster manager account queries
CREATE INDEX idx_users_role_status ON users(role_enum, status_enum);
CREATE INDEX idx_users_email_normalized ON users(normalized_email);
CREATE INDEX idx_users_created_at ON users(created_at DESC);

-- View for Manager Accounts (Admin and Staff)
CREATE OR REPLACE VIEW v_manager_accounts AS
SELECT 
    id,
    email,
    normalized_email,
    first_name,
    last_name,
    phone,
    date_of_birth,
    address,
    profile_image_url,
    role_enum,
    status_enum,
    created_at,
    updated_at,
    CASE 
        WHEN role_enum = 1 THEN 'Staff'
        WHEN role_enum = 2 THEN 'Admin'
        ELSE 'Unknown'
    END AS role_name,
    CASE 
        WHEN status_enum = 0 THEN 'Active'
        WHEN status_enum = 1 THEN 'Inactive'
        WHEN status_enum = 2 THEN 'Suspended'
        ELSE 'Unknown'
    END AS status_name
FROM users
WHERE role_enum IN (1, 2); -- Staff or Admin

-- Function to get manager account statistics
CREATE OR REPLACE FUNCTION get_manager_statistics()
RETURNS TABLE (
    total_managers BIGINT,
    total_admins BIGINT,
    total_staff BIGINT,
    active_managers BIGINT,
    inactive_managers BIGINT,
    suspended_managers BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*) FILTER (WHERE role_enum IN (1, 2)) AS total_managers,
        COUNT(*) FILTER (WHERE role_enum = 2) AS total_admins,
        COUNT(*) FILTER (WHERE role_enum = 1) AS total_staff,
        COUNT(*) FILTER (WHERE role_enum IN (1, 2) AND status_enum = 0) AS active_managers,
        COUNT(*) FILTER (WHERE role_enum IN (1, 2) AND status_enum = 1) AS inactive_managers,
        COUNT(*) FILTER (WHERE role_enum IN (1, 2) AND status_enum = 2) AS suspended_managers
    FROM users;
END;
$$ LANGUAGE plpgsql;

-- Function to search manager accounts
CREATE OR REPLACE FUNCTION search_manager_accounts(
    search_term TEXT DEFAULT NULL,
    p_role_enum INTEGER DEFAULT NULL,
    p_status_enum INTEGER DEFAULT NULL
)
RETURNS TABLE (
    id INTEGER,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    phone VARCHAR(20),
    role_enum INTEGER,
    status_enum INTEGER,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.id,
        u.email,
        u.first_name,
        u.last_name,
        u.phone,
        u.role_enum,
        u.status_enum,
        u.created_at
    FROM users u
    WHERE 
        u.role_enum IN (1, 2)
        AND (search_term IS NULL OR search_term = '' OR 
             LOWER(CONCAT(u.first_name, ' ', u.last_name)) LIKE LOWER('%' || search_term || '%') OR
             LOWER(u.email) LIKE LOWER('%' || search_term || '%'))
        AND (p_role_enum IS NULL OR u.role_enum = p_role_enum)
        AND (p_status_enum IS NULL OR u.status_enum = p_status_enum)
    ORDER BY u.created_at DESC;
END;
$$ LANGUAGE plpgsql;

-- Function to get manager account by ID with validation
CREATE OR REPLACE FUNCTION get_manager_account_by_id(p_user_id INTEGER)
RETURNS TABLE (
    id INTEGER,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    phone VARCHAR(20),
    address TEXT,
    date_of_birth DATE,
    profile_image_url VARCHAR(500),
    role_enum INTEGER,
    status_enum INTEGER,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.id,
        u.email,
        u.first_name,
        u.last_name,
        u.phone,
        u.address,
        u.date_of_birth,
        u.profile_image_url,
        u.role_enum,
        u.status_enum,
        u.created_at,
        u.updated_at
    FROM users u
    WHERE u.id = p_user_id
        AND u.role_enum IN (1, 2);
END;
$$ LANGUAGE plpgsql;

-- Function to create manager account
CREATE OR REPLACE FUNCTION create_manager_account(
    p_email VARCHAR(255),
    p_normalized_email VARCHAR(255),
    p_password_hash VARCHAR(255),
    p_password_salt VARCHAR(255),
    p_first_name VARCHAR(100),
    p_last_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_address TEXT,
    p_date_of_birth DATE,
    p_role_enum INTEGER
)
RETURNS TABLE (
    id INTEGER,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role_enum INTEGER,
    status_enum INTEGER
) AS $$
DECLARE
    new_id INTEGER;
BEGIN
    -- Insert new manager account
    INSERT INTO users (
        email, normalized_email, password_hash, password_salt,
        first_name, last_name, phone, address, date_of_birth,
        role_enum, status_enum, created_at, updated_at
    ) VALUES (
        p_email, p_normalized_email, p_password_hash, p_password_salt,
        p_first_name, p_last_name, p_phone, p_address, p_date_of_birth,
        p_role_enum, 0, NOW(), NOW()
    )
    RETURNING users.id INTO new_id;

    -- Return created account
    RETURN QUERY
    SELECT 
        u.id,
        u.email,
        u.first_name,
        u.last_name,
        u.role_enum,
        u.status_enum
    FROM users u
    WHERE u.id = new_id;
END;
$$ LANGUAGE plpgsql;

-- Function to update manager account
CREATE OR REPLACE FUNCTION update_manager_account(
    p_user_id INTEGER,
    p_first_name VARCHAR(100),
    p_last_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_address TEXT,
    p_date_of_birth DATE,
    p_role_enum INTEGER,
    p_status_enum INTEGER
)
RETURNS TABLE (
    id INTEGER,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role_enum INTEGER,
    status_enum INTEGER,
    updated_at TIMESTAMP
) AS $$
BEGIN
    -- Update manager account
    UPDATE users 
    SET 
        first_name = COALESCE(p_first_name, first_name),
        last_name = COALESCE(p_last_name, last_name),
        phone = COALESCE(p_phone, phone),
        address = COALESCE(p_address, address),
        date_of_birth = COALESCE(p_date_of_birth, date_of_birth),
        role_enum = COALESCE(p_role_enum, role_enum),
        status_enum = COALESCE(p_status_enum, status_enum),
        updated_at = NOW()
    WHERE id = p_user_id
        AND role_enum IN (1, 2);

    -- Return updated account
    RETURN QUERY
    SELECT 
        u.id,
        u.email,
        u.first_name,
        u.last_name,
        u.role_enum,
        u.status_enum,
        u.updated_at
    FROM users u
    WHERE u.id = p_user_id;
END;
$$ LANGUAGE plpgsql;

-- Function to change manager account status
CREATE OR REPLACE FUNCTION change_manager_status(
    p_user_id INTEGER,
    p_status_enum INTEGER
)
RETURNS TABLE (
    id INTEGER,
    email VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role_enum INTEGER,
    status_enum INTEGER,
    updated_at TIMESTAMP
) AS $$
BEGIN
    -- Update status
    UPDATE users 
    SET 
        status_enum = p_status_enum,
        updated_at = NOW()
    WHERE id = p_user_id
        AND role_enum IN (1, 2);

    -- Return updated account
    RETURN QUERY
    SELECT 
        u.id,
        u.email,
        u.first_name,
        u.last_name,
        u.role_enum,
        u.status_enum,
        u.updated_at
    FROM users u
    WHERE u.id = p_user_id;
END;
$$ LANGUAGE plpgsql;

-- Function to check if email exists
CREATE OR REPLACE FUNCTION check_email_exists(p_email VARCHAR(255))
RETURNS BOOLEAN AS $$
DECLARE
    exists_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO exists_count
    FROM users
    WHERE normalized_email = UPPER(p_email);
    
    RETURN exists_count > 0;
END;
$$ LANGUAGE plpgsql;

-- Trigger to automatically update normalized_email when email changes
CREATE OR REPLACE FUNCTION normalize_email_trigger()
RETURNS TRIGGER AS $$
BEGIN
    NEW.normalized_email := UPPER(NEW.email);
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_normalize_email
    BEFORE INSERT OR UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION normalize_email_trigger();

-- Add comments to the functions
COMMENT ON VIEW v_manager_accounts IS 'View of all manager accounts (Admin and Staff)';
COMMENT ON FUNCTION get_manager_statistics() IS 'Get statistics about manager accounts';
COMMENT ON FUNCTION search_manager_accounts(TEXT, INTEGER, INTEGER) IS 'Search manager accounts with filters';
COMMENT ON FUNCTION get_manager_account_by_id(INTEGER) IS 'Get a specific manager account by ID';
COMMENT ON FUNCTION create_manager_account IS 'Create a new manager account';
COMMENT ON FUNCTION update_manager_account IS 'Update manager account information';
COMMENT ON FUNCTION change_manager_status IS 'Change manager account status';
COMMENT ON FUNCTION check_email_exists(VARCHAR) IS 'Check if an email already exists in the system';