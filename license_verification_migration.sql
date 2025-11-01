-- Migration script to add license verification fields to driving_licenses table
-- Add new columns for license verification
ALTER TABLE driving_licenses
ADD COLUMN verification_status INTEGER DEFAULT 0 NOT NULL,
    ADD COLUMN reject_reason TEXT,
    ADD COLUMN verified_by_user_id INTEGER,
    ADD COLUMN verified_at TIMESTAMP;
-- Add foreign key constraint for verified_by_user_id
ALTER TABLE driving_licenses
ADD CONSTRAINT fk_driving_licenses_verified_by_user_id FOREIGN KEY (verified_by_user_id) REFERENCES users(id) ON DELETE
SET NULL;
-- Add index for better query performance
CREATE INDEX idx_driving_licenses_verification_status ON driving_licenses(verification_status);
CREATE INDEX idx_driving_licenses_verified_by_user_id ON driving_licenses(verified_by_user_id);
-- Add comments for documentation
COMMENT ON COLUMN driving_licenses.verification_status IS 'License verification status: 0=Pending, 1=Verified, 2=Rejected, 3=Expired';
COMMENT ON COLUMN driving_licenses.reject_reason IS 'Reason for rejection if license is rejected';
COMMENT ON COLUMN driving_licenses.verified_by_user_id IS 'ID of admin/staff who verified the license';
COMMENT ON COLUMN driving_licenses.verified_at IS 'Timestamp when license was verified';