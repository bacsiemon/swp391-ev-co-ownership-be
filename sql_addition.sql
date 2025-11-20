-- ==============================================================================
-- CONTRACT MANAGEMENT TABLES
-- ==============================================================================
-- Additional tables for contract template and group contract management
-- Created: 2025-11-20
-- ==============================================================================

-- CONTRACT RELATED ENUMS
-- contract_template_status_enum: 0=Draft, 1=Active, 2=Inactive, 3=Archived
-- group_contract_status_enum:    0=Draft, 1=Active, 2=Expired, 3=Terminated, 4=Cancelled

-- ==============================================================================
-- Contract Templates Table
-- ==============================================================================
-- Stores reusable contract templates for groups
-- ------------------------------------------------------------------------------

CREATE TABLE contract_templates (
	id SERIAL PRIMARY KEY,
	name VARCHAR(200) NOT NULL,
	version VARCHAR(50) NOT NULL,
	description TEXT,
	content TEXT NOT NULL,
	terms_and_conditions TEXT,
	status_enum INTEGER DEFAULT 0,                   -- contract_template_status_enum: 0=Draft, 1=Active, 2=Inactive, 3=Archived
	created_by INTEGER REFERENCES users(id),
	approved_by INTEGER REFERENCES users(id),
	approved_at TIMESTAMP,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- ==============================================================================
-- Group Contracts Table
-- ==============================================================================
-- Stores actual contracts for groups with snapshot of template
-- ------------------------------------------------------------------------------

CREATE TABLE group_contracts (
	id SERIAL PRIMARY KEY,
	group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
	
	-- Snapshot of contract template at time of creation
	template_id INTEGER REFERENCES contract_templates(id),
	name VARCHAR(200) NOT NULL,
	version VARCHAR(50) NOT NULL,
	content TEXT NOT NULL,
	terms_and_conditions TEXT,
	
	-- Contract specific information
	status_enum INTEGER DEFAULT 0,                   -- group_contract_status_enum: 0=Draft, 1=Active, 2=Expired, 3=Terminated, 4=Cancelled
	
	-- Date management
	effective_date DATE NOT NULL,
	expiry_date DATE,
	termination_date DATE,
	
	-- Approval and signature tracking
	created_by INTEGER REFERENCES users(id),
	approved_by INTEGER REFERENCES users(id),
	approved_at TIMESTAMP,
	
	-- Additional metadata
	notes TEXT,
	attachment_urls TEXT,
	
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP DEFAULT NOW()
);

-- ==============================================================================
-- SAMPLE DATA - CONTRACT TEMPLATES
-- ==============================================================================
-- Sample contract templates for testing
-- ------------------------------------------------------------------------------

INSERT INTO contract_templates (
		name,
		version,
		description,
		content,
		terms_and_conditions,
		status_enum,
		created_by,
		approved_by,
		approved_at
	)
VALUES (
		'Standard Vehicle Co-Ownership Agreement',
		'v1.0',
		'Standard template for electric vehicle co-ownership contracts',
		'VEHICLE CO-OWNERSHIP AGREEMENT

This agreement is made between the co-owners listed below for the purpose of shared ownership and operation of an electric vehicle.

1. PARTIES
   The parties to this agreement are the members of the co-ownership group.

2. VEHICLE DETAILS
   The vehicle subject to this agreement shall be identified in the group vehicle records.

3. OWNERSHIP SHARES
   Each co-owner holds an ownership percentage as specified in the group membership records.

4. RESPONSIBILITIES
   - All co-owners agree to share costs proportionally to their ownership percentage
   - Regular maintenance shall be performed as scheduled
   - Insurance and registration fees shall be shared among co-owners

5. USAGE RIGHTS
   - Co-owners may book the vehicle according to the booking system rules
   - Priority shall be given based on booking time and ownership percentage
   - Emergency use may override regular bookings with group consent

6. FINANCIAL OBLIGATIONS
   - Monthly contributions to the vehicle fund are mandatory
   - Extraordinary expenses require group vote approval
   - Late payment penalties apply as per platform rules',
		'TERMS AND CONDITIONS

1. This agreement is valid for the duration specified in the contract.
2. Any party may exit the agreement with 30 days notice.
3. Disputes shall be resolved through mediation.
4. Changes to this agreement require unanimous consent.
5. This agreement is governed by Vietnamese law.',
		1,
		1,
		1,
		NOW() - INTERVAL '30 days'
	),
	(
		'Maintenance Service Agreement',
		'v1.0',
		'Template for regular vehicle maintenance agreements',
		'VEHICLE MAINTENANCE AGREEMENT

This agreement establishes the terms for regular maintenance services for the group-owned vehicle.

1. SCOPE OF SERVICES
   - Regular inspection and maintenance
   - Battery health monitoring
   - Software updates
   - Emergency repairs as needed

2. SERVICE SCHEDULE
   - Monthly inspections
   - Quarterly comprehensive checks
   - Annual major service

3. COST ALLOCATION
   All maintenance costs shall be shared according to ownership percentages.

4. SERVICE PROVIDERS
   Only authorized service centers shall be used for maintenance.',
		'Standard platform terms and conditions apply.',
		1,
		1,
		1,
		NOW() - INTERVAL '20 days'
	);

-- ==============================================================================
-- SAMPLE DATA - GROUP CONTRACTS
-- ==============================================================================
-- Sample group contracts for testing
-- ------------------------------------------------------------------------------

INSERT INTO group_contracts (
		group_id,
		template_id,
		name,
		version,
		content,
		terms_and_conditions,
		status_enum,
		effective_date,
		expiry_date,
		created_by,
		approved_by,
		approved_at
	)
VALUES (
		1,
		1,
		'Standard Vehicle Co-Ownership Agreement',
		'v1.0',
		'VEHICLE CO-OWNERSHIP AGREEMENT

This agreement is made between the co-owners listed below for the purpose of shared ownership and operation of an electric vehicle.

1. PARTIES
   The parties to this agreement are the members of the co-ownership group.

2. VEHICLE DETAILS
   Vehicle: Tesla Model 3 (License Plate: 51A-12345)

3. OWNERSHIP SHARES
   - John Doe: 55%
   - Jane Smith: 45%

4. RESPONSIBILITIES
   - All co-owners agree to share costs proportionally to their ownership percentage
   - Regular maintenance shall be performed as scheduled
   - Insurance and registration fees shall be shared among co-owners

5. USAGE RIGHTS
   - Co-owners may book the vehicle according to the booking system rules
   - Priority shall be given based on booking time and ownership percentage
   - Emergency use may override regular bookings with group consent

6. FINANCIAL OBLIGATIONS
   - Monthly contributions to the vehicle fund are mandatory
   - Extraordinary expenses require group vote approval
   - Late payment penalties apply as per platform rules',
		'TERMS AND CONDITIONS

1. This agreement is valid for the duration specified in the contract.
2. Any party may exit the agreement with 30 days notice.
3. Disputes shall be resolved through mediation.
4. Changes to this agreement require unanimous consent.
5. This agreement is governed by Vietnamese law.',
		1,
		'2024-06-15',
		'2025-06-15',
		1,
		1,
		NOW() - INTERVAL '25 days'
	),
	(
		2,
		1,
		'Standard Vehicle Co-Ownership Agreement',
		'v1.0',
		'VEHICLE CO-OWNERSHIP AGREEMENT

This agreement is made between the co-owners listed below for the purpose of shared ownership and operation of an electric vehicle.

1. PARTIES
   The parties to this agreement are the members of the co-ownership group.

2. VEHICLE DETAILS
   Vehicle: VinFast VF8 (License Plate: 51B-67890)

3. OWNERSHIP SHARES
   - John Doe: 60%
   - Mike Wilson: 40%

4. RESPONSIBILITIES
   - All co-owners agree to share costs proportionally to their ownership percentage
   - Regular maintenance shall be performed as scheduled
   - Insurance and registration fees shall be shared among co-owners

5. USAGE RIGHTS
   - Co-owners may book the vehicle according to the booking system rules
   - Priority shall be given based on booking time and ownership percentage
   - Emergency use may override regular bookings with group consent

6. FINANCIAL OBLIGATIONS
   - Monthly contributions to the vehicle fund are mandatory
   - Extraordinary expenses require group vote approval
   - Late payment penalties apply as per platform rules',
		'TERMS AND CONDITIONS

1. This agreement is valid for the duration specified in the contract.
2. Any party may exit the agreement with 30 days notice.
3. Disputes shall be resolved through mediation.
4. Changes to this agreement require unanimous consent.
5. This agreement is governed by Vietnamese law.',
		1,
		'2024-01-20',
		'2025-01-20',
		1,
		1,
		NOW() - INTERVAL '15 days'
	);

-- Commit changes
COMMIT;

-- ==============================================================================
-- END OF CONTRACT TABLES ADDITION
-- ==============================================================================
