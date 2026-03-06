-- IndyPOS StoreHub Database Initialization
-- This script runs automatically when the PostgreSQL container starts

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE indypos_storehub TO indypos_app;

-- Create schemas (will be created by EF Core migrations)
-- This is just a placeholder for initial setup

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'IndyPOS StoreHub database initialized successfully';
END $$;
