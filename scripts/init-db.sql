-- ==================================================
-- AlfTekPro HRMS - Database Initialization Script
-- ==================================================
-- This script runs automatically when PostgreSQL container starts for the first time
-- It sets up necessary extensions and configurations

\connect alftekpro_hrms;

-- Enable UUID generation (required for primary keys)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable pgcrypto for password hashing and encryption functions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Enable PostgreSQL Full Text Search enhancements
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Enable case-insensitive text (useful for email lookups)
CREATE EXTENSION IF NOT EXISTS "citext";

-- Create custom types (if needed)
-- None required for MVP

-- Set timezone to UTC (critical for multi-region support)
SET timezone = 'UTC';

-- Create audit trigger function (for automatic updated_at timestamps)
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Log completion
DO $$
BEGIN
    RAISE NOTICE 'Database initialization completed successfully';
    RAISE NOTICE 'Extensions installed: uuid-ossp, pgcrypto, pg_trgm, citext';
    RAISE NOTICE 'Timezone set to: UTC';
END $$;
