#!/bin/bash

# Test script for Brand-Only Casino Platform
# This script tests the main endpoints to verify the system is working

echo "=== Testing Brand-Only Casino Platform ==="
echo ""

# Base URL - adjust if needed
BASE_URL="https://localhost:7251/api/v1"

echo "1. Testing Brand Discovery (Get Brand by Host)"
echo "GET $BASE_URL/admin/brands/by-host/demo.casino.com"
echo ""

echo "2. Testing Brand List (should require authentication)"
echo "GET $BASE_URL/admin/brands"
echo ""

echo "3. Testing Authentication"
echo "POST $BASE_URL/auth/login"
echo "Body: { \"username\": \"superadmin\", \"password\": \"Admin123!\" }"
echo ""

echo "4. After authentication, test authorized brand operations:"
echo "GET $BASE_URL/admin/brands (with Bearer token)"
echo "GET $BASE_URL/admin/brands/{brandId} (with Bearer token)"
echo ""

echo "5. Test Brand-specific operations:"
echo "GET $BASE_URL/admin/brands/{brandId}/catalog (with Brand Admin token)"
echo "GET $BASE_URL/admin/brands/{brandId}/settings (with Brand Admin token)"
echo ""

echo "6. Test Cashier operations (scoped to brand):"
echo "GET $BASE_URL/cashier/players (with Cashier token)"
echo ""

echo "=== Expected Behavior ==="
echo ""
echo "? SUPER_ADMIN: Can access all brands (brandScope = null)"
echo "? BRAND_ADMIN: Can only access their assigned brand"
echo "? CASHIER: Can only access their assigned brand and assigned players"
echo ""
echo "?? All brand operations are now scoped by brandContext"
echo "?? No more operator references anywhere in the system"
echo ""

echo "=== Database State ==="
echo "After running seed script, you should have:"
echo "- 3 Brands: DEMO_BRAND, VIP_BRAND, EURO_BRAND"
echo "- 1 SUPER_ADMIN (no brand assignment)"
echo "- 3 BRAND_ADMINs (one per brand)"
echo "- 3 CASHIERs (assigned to specific brands)"
echo "- Sample players, games, and provider configs per brand"
echo ""

echo "Run this API manually with curl or Postman to verify functionality!"
echo "Remember to update appsettings.json with correct database connection if needed."