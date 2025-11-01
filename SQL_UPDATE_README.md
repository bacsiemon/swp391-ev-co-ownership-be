# Database Schema Update - November 1, 2025

## Overview
Updated `sql.sql` file to match current codebase and remove deprecated/unused tables to create a clean, production-ready database schema for the EV Co-Ownership system.

## 🔄 Changes Made

### ✅ **Enhanced Existing Tables**

#### `driving_licenses` Table
- **Added missing fields** from current codebase:
  - `reject_reason TEXT` - Reason for license rejection
  - `verified_by_user_id INTEGER` - Staff/Admin who verified
  - `verified_at TIMESTAMP` - Verification timestamp
- **Updated enum comments** to match `EDrivingLicenseVerificationStatus`
- **Fixed column name** from `verification_status_enum` to `verification_status`

#### `vehicle_usage_records` Table ⭐ **NEW**
- **Completely new table** that exists in code but was missing from SQL
- Tracks complete trip records from check-in to check-out
- Includes cost calculations, battery usage, odometer readings
- Links bookings with actual usage data for analytics

#### `vehicle_upgrade_proposals` Table ⭐ **NEW**
- **Completely new table** for upgrade proposal system
- Voting mechanism for vehicle improvements
- Cost tracking and execution status
- Integration with fund usage system

#### `vehicle_upgrade_votes` Table ⭐ **NEW**
- **Voting system** for upgrade proposals
- Co-owner consensus tracking
- Comment support for votes

### ❌ **Removed Deprecated Tables**
The following tables were removed as they are **not used** in the current codebase:
- `groups` - Not implemented in current system
- `group_members` - Not implemented in current system  
- `votes` - Replaced by specific voting tables
- `vote_options` - Replaced by specific voting tables
- `vote_results` - Replaced by specific voting tables
- `vehicle_verification_history` - Commented out in models

### 📈 **Performance Optimizations**

#### New Indexes Added
```sql
-- User performance
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role_status ON users(role_enum, status_enum);

-- Vehicle performance  
CREATE INDEX idx_vehicles_status ON vehicles(status_enum);
CREATE INDEX idx_bookings_status ON bookings(status_enum);

-- Usage tracking
CREATE INDEX idx_vehicle_usage_records_vehicle_id ON vehicle_usage_records(vehicle_id);
CREATE INDEX idx_vehicle_usage_records_start_time ON vehicle_usage_records(start_time);

-- And 15+ more strategic indexes for optimal query performance
```

### 🎯 **Code Alignment Fixes**

#### Enum Updates
- **Updated all enum comments** to match C# enum values exactly
- **Fixed casing** from `snake_case` to `PascalCase` to match .NET conventions
- **Added new enums** for upgrade system

#### Column Consistency  
- **Fixed foreign key references** to match current entity relationships
- **Added missing NOT NULL constraints** where appropriate
- **Standardized data types** across related tables

### 🧪 **Development Support**

#### Sample Data
```sql
-- Default admin user (admin@evco.com / Admin123!)
-- Default staff user (staff@evco.com / Staff123!)  
-- System configuration values
-- Ready-to-use test accounts
```

#### Database Tools
- **Clean schema drop/create** for fresh installations
- **Comprehensive documentation** with enum mappings
- **Index strategy** for production performance

## 📊 **Database Schema Summary**

### Core Tables (14)
- `users`, `user_refresh_tokens`, `co_owners`, `driving_licenses`
- `vehicles`, `vehicle_co_owners`, `vehicle_stations`, `vehicle_conditions`
- `funds`, `fund_additions`, `fund_usage`, `fund_usage_votes`
- `payments`, `configurations`

### Booking & Usage (4) 
- `bookings`, `check_ins`, `check_outs`, `vehicle_usage_records`

### Maintenance & Upgrades (3)
- `maintenance_costs`, `vehicle_upgrade_proposals`, `vehicle_upgrade_votes`

### System Features (3)
- `notification_entities`, `user_notifications`, `file_uploads`

## 🚀 **Production Ready**

✅ **PostgreSQL optimized** with proper data types  
✅ **Entity Framework Core compatible** column naming  
✅ **Performance indexed** for common queries  
✅ **Clean architecture aligned** with current codebase  
✅ **No deprecated dependencies** - only active features  

## 🔧 **Next Steps**

1. **Run the new `sql.sql`** on fresh PostgreSQL database
2. **Update Entity Framework** with `dotnet ef database update`
3. **Test with sample data** using provided default accounts
4. **Deploy to production** with confidence

## 📝 **Migration Notes**

If migrating from old schema:
```sql
-- Backup existing data first
-- Drop deprecated tables manually if they exist  
-- Run new schema creation
-- Migrate data to new vehicle_usage_records table if needed
```

---
**Generated:** November 1, 2025  
**Compatibility:** PostgreSQL 12+, .NET 8, EF Core 8  
**Status:** ✅ Production Ready