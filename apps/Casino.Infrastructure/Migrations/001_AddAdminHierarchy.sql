-- Migration: 001_AddAdminHierarchy.sql
-- Purpose: Add multilevel hierarchy support to BackofficeUsers
-- Date: 2025-01-22
-- Author: System Migration

BEGIN;

-- 1. Add new columns for hierarchy
ALTER TABLE "BackofficeUsers"
    ADD COLUMN parent_admin_id UUID REFERENCES "BackofficeUsers"("Id"),
    ADD COLUMN hierarchy_level INTEGER DEFAULT 0,
    ADD COLUMN hierarchy_path TEXT;

-- 2. Create indexes for performance
CREATE INDEX idx_backoffice_parent_admin 
    ON "BackofficeUsers"(parent_admin_id) 
    WHERE parent_admin_id IS NOT NULL;

CREATE INDEX idx_backoffice_hierarchy_path 
    ON "BackofficeUsers" USING btree(hierarchy_path text_pattern_ops);

-- 3. Backfill existing data from ParentCashierId
UPDATE "BackofficeUsers"
SET 
    parent_admin_id = "ParentCashierId",
    hierarchy_level = CASE
        WHEN "Role" = 'SUPER_ADMIN' THEN 0
        WHEN "Role" = 'BRAND_ADMIN' THEN 1
        WHEN "Role" = 'CASHIER' AND "ParentCashierId" IS NOT NULL THEN 2
        ELSE 1
    END,
    hierarchy_path = CASE
        WHEN "Role" = 'SUPER_ADMIN' THEN '.root.'
        WHEN "Role" = 'BRAND_ADMIN' THEN '.root.' || "Id"::TEXT || '.'
        WHEN "Role" = 'CASHIER' AND "ParentCashierId" IS NOT NULL THEN
            (SELECT '.root.' || p."Id"::TEXT || '.' || "BackofficeUsers"."Id"::TEXT || '.'
             FROM "BackofficeUsers" p 
             WHERE p."Id" = "BackofficeUsers"."ParentCashierId")
        ELSE '.root.' || "Id"::TEXT || '.'
    END
WHERE "Status" = 'ACTIVE';

-- 4. Create function to auto-update hierarchy_path
CREATE OR REPLACE FUNCTION update_hierarchy_path()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.parent_admin_id IS NULL THEN
        NEW.hierarchy_path := '.root.';
        NEW.hierarchy_level := 0;
    ELSE
        SELECT 
            p.hierarchy_path || NEW."Id"::TEXT || '.',
            p.hierarchy_level + 1
        INTO NEW.hierarchy_path, NEW.hierarchy_level
        FROM "BackofficeUsers" p
        WHERE p."Id" = NEW.parent_admin_id;
        
        -- Fallback if parent not found
        IF NEW.hierarchy_path IS NULL THEN
            NEW.hierarchy_path := '.root.' || NEW."Id"::TEXT || '.';
            NEW.hierarchy_level := 1;
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 5. Create trigger
CREATE TRIGGER trg_update_hierarchy_path
    BEFORE INSERT OR UPDATE OF parent_admin_id ON "BackofficeUsers"
    FOR EACH ROW
    EXECUTE FUNCTION update_hierarchy_path();

-- 6. Add constraint to prevent circular references
CREATE OR REPLACE FUNCTION check_no_circular_hierarchy()
RETURNS TRIGGER AS $$
DECLARE
    visited UUID[];
    current_id UUID;
    max_depth INTEGER := 10; -- Prevent infinite loops
    depth INTEGER := 0;
BEGIN
    IF NEW.parent_admin_id IS NULL THEN
        RETURN NEW;
    END IF;
    
    IF NEW.parent_admin_id = NEW."Id" THEN
        RAISE EXCEPTION 'User cannot be their own parent';
    END IF;
    
    visited := ARRAY[NEW."Id"];
    current_id := NEW.parent_admin_id;
    
    WHILE current_id IS NOT NULL AND depth < max_depth LOOP
        IF current_id = ANY(visited) THEN
            RAISE EXCEPTION 'Circular reference detected in admin hierarchy';
        END IF;
        
        visited := array_append(visited, current_id);
        depth := depth + 1;
        
        SELECT parent_admin_id INTO current_id
        FROM "BackofficeUsers"
        WHERE "Id" = current_id;
    END LOOP;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_circular_hierarchy
    BEFORE INSERT OR UPDATE OF parent_admin_id ON "BackofficeUsers"
    FOR EACH ROW
    EXECUTE FUNCTION check_no_circular_hierarchy();

-- 7. Validation: Show backfilled data
SELECT 
    "Id",
    "Username",
    "Role",
    parent_admin_id,
    hierarchy_level,
    hierarchy_path
FROM "BackofficeUsers"
WHERE "Status" = 'ACTIVE'
ORDER BY hierarchy_level, "Username";

COMMIT;

-- Success message
DO $$
BEGIN
    RAISE NOTICE '? Migration 001_AddAdminHierarchy completed successfully';
    RAISE NOTICE 'Added columns: parent_admin_id, hierarchy_level, hierarchy_path';
    RAISE NOTICE 'Created indexes, functions, and triggers';
    RAISE NOTICE 'Backfilled data from existing ParentCashierId';
END $$;
