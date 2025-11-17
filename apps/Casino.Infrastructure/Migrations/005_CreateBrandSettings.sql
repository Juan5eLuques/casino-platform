-- Migration: Add BrandSettings table
-- Date: 2025-01-XX
-- Description: Add BrandSettings table for managing brand colors and images (banners, logos, media)

-- Create BrandSettings table
CREATE TABLE "BrandSettings" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "BrandId" uuid NOT NULL,
 "Colors" jsonb NOT NULL DEFAULT '{}'::jsonb,
  "Images" jsonb NOT NULL DEFAULT '{"banners":{"home":[],"slots":[],"live-casino":[]},"media":{"logo":"","favicon":"","others":[]}}'::jsonb,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "FK_BrandSettings_Brands_BrandId" FOREIGN KEY ("BrandId") 
    REFERENCES "Brands" ("Id") ON DELETE CASCADE
);

-- Create unique index on BrandId
CREATE UNIQUE INDEX "IX_BrandSettings_BrandId" ON "BrandSettings" ("BrandId");

-- Add comment
COMMENT ON TABLE "BrandSettings" IS 'Brand settings for colors, banners, logos and media assets stored in S3';
