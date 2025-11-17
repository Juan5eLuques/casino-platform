# Brand Assets System - Integration Test Examples

## Prerequisites

- Brand created in system (e.g., "bet30")
- JWT token obtained from login
- Brand domain configured (e.g., "bet30.com")
- Test images available

## Environment Variables

```bash
export API_URL="https://your-api.com"
export JWT_TOKEN="your_jwt_token_here"
export BRAND_HOST="bet30.com"
```

## Test 1: Initialize Brand Assets

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/initialize" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -H "Content-Type: application/json" \
  -v

# Expected Response (200 OK):
{
  "success": true,
  "message": "Brand assets initialized successfully",
  "foldersCreated": [
    "assets/bet30/banners/home/",
    "assets/bet30/banners/slots/",
    "assets/bet30/banners/live-casino/",
    "assets/bet30/banners/media/",
    "assets/bet30/config/"
  ]
}
```

## Test 2: Upload Logo

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@test-logo.png" \
  -v

# Expected Response (200 OK):
{
  "success": true,
  "url": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/logo.png",
  "type": "logo",
  "fileName": "logo.png"
}
```

## Test 3: Upload Favicon

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/favicon" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@test-favicon.ico" \
  -v

# Expected Response (200 OK):
{
  "success": true,
  "url": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/favicon.ico",
  "type": "favicon",
  "fileName": "favicon.ico"
}
```

## Test 4: Upload Home Banners

```bash
# Banner 1
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/home" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@banner1.jpg" \
  -v

# Banner 2
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/home" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@banner2.jpg" \
  -v

# Expected Response for each (200 OK):
{
  "success": true,
  "url": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/abc-123.jpg",
  "section": "home",
  "fileName": "abc-123.jpg",
  "totalBannersInSection": 1
}
```

## Test 5: Upload Slots Banners

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/slots" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@slots-banner.jpg" \
  -v
```

## Test 6: Upload Live Casino Banners

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/live-casino" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@live-casino-banner.jpg" \
  -v
```

## Test 7: Update Colors

```bash
curl -X PUT "$API_URL/api/v1/admin/brands/assets/colors" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -H "Content-Type: application/json" \
  -d '{
    "colors": {
      "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3",
      "--color-accent": "#e91e63",
      "--color-background": "#121212",
      "--color-surface": "#1e1e1e",
      "--color-text": "#ffffff",
 "--color-text-secondary": "#b0b0b0"
    }
  }' \
  -v

# Expected Response (200 OK): Full brand settings object
```

## Test 8: Get Brand Settings

```bash
curl -X GET "$API_URL/api/v1/admin/brands/assets/settings" \
  -H "Authorization: Bearer $JWT_TOKEN" \
-H "Host: $BRAND_HOST" \
  -v

# Expected Response (200 OK):
{
  "brandId": "123e4567-e89b-12d3-a456-426614174000",
  "brandName": "Bet30 Casino",
  "brandCode": "bet30",
  "colors": {
    "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3",
    "--color-accent": "#e91e63"
  },
  "banners": {
    "home": [
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/banner1.jpg",
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/banner2.jpg"
    ],
    "slots": [
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/slots/slots-banner.jpg"
    ],
    "liveCasino": [
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/live-casino/live-casino-banner.jpg"
 ]
  },
  "media": {
    "logo": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/logo.png",
    "favicon": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/favicon.ico",
    "others": []
  },
  "configJsUrl": null,
  "updatedAt": "2025-01-15T10:30:00Z"
}
```

## Test 9: Publish Config.js

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/publish-config" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -v

# Expected Response (200 OK):
{
  "success": true,
  "configUrl": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js",
  "publishedAt": "2025-01-15T10:35:00Z"
}
```

## Test 10: Verify Config.js

```bash
curl "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js" \
  -v

# Expected Response (200 OK):
// Brand Configuration - Auto-generated
// Brand: Bet30 Casino (bet30)
// Generated: 2025-01-15 10:35:00 UTC

window.gBrandName = "bet30";

window.gColors = {
  "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3",
  "--color-accent": "#e91e63"
};

window.gHomeBannersDesktop = ["https://...", "https://..."];
window.gSlotsBannersDesktop = ["https://..."];
window.gLiveCasinoBannersDesktop = ["https://..."];

window.gLogo = "https://...";
window.gFavicon = "https://...";
```

## Test 11: Delete Banner

```bash
# Get the fileName from the settings response
BANNER_FILE="abc-123.jpg"

curl -X DELETE "$API_URL/api/v1/admin/brands/assets/banner/home/$BANNER_FILE" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -v

# Expected Response (200 OK):
{
  "success": true,
  "message": "Banner deleted successfully"
}
```

## Test 12: Delete Media (Logo)

```bash
curl -X DELETE "$API_URL/api/v1/admin/brands/assets/media/logo" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -v

# Expected Response (200 OK):
{
  "success": true,
"message": "Media deleted successfully"
}
```

## Test 13: Validation Tests

### Test Max File Size (Should Fail)

```bash
# Create a 6MB file (exceeds 5MB limit)
dd if=/dev/zero of=large-file.jpg bs=1M count=6

curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@large-file.jpg" \
  -v

# Expected Response (400 Bad Request):
{
  "error": "File size exceeds 5MB limit"
}
```

### Test Invalid File Type (Should Fail)

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@document.pdf" \
  -v

# Expected Response (400 Bad Request):
{
  "error": "Invalid file extension. Allowed: .jpg, .jpeg, .png, .gif, .webp, .svg"
}
```

### Test Max Banners Per Section (Should Fail after 5)

```bash
# Upload 5 banners first...
for i in {1..5}; do
  curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/home" \
    -H "Authorization: Bearer $JWT_TOKEN" \
    -H "Host: $BRAND_HOST" \
    -F "file=@banner$i.jpg"
done

# Try to upload 6th banner (should fail)
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/home" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@banner6.jpg" \
  -v

# Expected Response (400 Bad Request):
{
  "error": "Maximum 5 banners per section reached"
}
```

### Test Invalid Section (Should Fail)

```bash
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/invalid-section" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@banner.jpg" \
  -v

# Expected Response (400 Bad Request):
{
  "error": "Invalid section. Must be one of: home, slots, live-casino"
}
```

## Test 14: Authentication Tests

### Test Without JWT (Should Fail)

```bash
curl -X GET "$API_URL/api/v1/admin/brands/assets/settings" \
  -H "Host: $BRAND_HOST" \
  -v

# Expected Response (401 Unauthorized)
```

### Test With Invalid JWT (Should Fail)

```bash
curl -X GET "$API_URL/api/v1/admin/brands/assets/settings" \
-H "Authorization: Bearer invalid_token" \
  -H "Host: $BRAND_HOST" \
  -v

# Expected Response (401 Unauthorized)
```

## Test 15: Brand Context Tests

### Test Without Host Header (Should Fail)

```bash
curl -X GET "$API_URL/api/v1/admin/brands/assets/settings" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -v

# Expected Response (400 Bad Request):
{
  "error": "Brand context not resolved"
}
```

## Complete Test Script

```bash
#!/bin/bash

# Configuration
API_URL="https://your-api.com"
JWT_TOKEN="your_jwt_token_here"
BRAND_HOST="bet30.com"

echo "=== Brand Assets Integration Tests ==="
echo ""

# Test 1: Initialize
echo "1. Initializing brand assets..."
curl -X POST "$API_URL/api/v1/admin/brands/assets/initialize" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -s | jq '.'
echo ""

# Test 2: Upload Logo
echo "2. Uploading logo..."
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@test-logo.png" \
  -s | jq '.'
echo ""

# Test 3: Upload Favicon
echo "3. Uploading favicon..."
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/favicon" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@test-favicon.ico" \
  -s | jq '.'
echo ""

# Test 4: Upload Banners
echo "4. Uploading banners..."
for i in {1..3}; do
  echo "   Uploading home banner $i..."
  curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/banner/home" \
    -H "Authorization: Bearer $JWT_TOKEN" \
    -H "Host: $BRAND_HOST" \
    -F "file=@banner$i.jpg" \
    -s | jq '.success'
done
echo ""

# Test 5: Update Colors
echo "5. Updating colors..."
curl -X PUT "$API_URL/api/v1/admin/brands/assets/colors" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
-H "Content-Type: application/json" \
  -d '{
    "colors": {
      "--color-primary": "#ffb300",
      "--color-secondary": "#2196f3",
      "--color-accent": "#e91e63"
    }
  }' \
  -s | jq '.colors'
echo ""

# Test 6: Publish Config
echo "6. Publishing config.js..."
curl -X POST "$API_URL/api/v1/admin/brands/assets/publish-config" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -s | jq '.'
echo ""

# Test 7: Get Settings
echo "7. Getting brand settings..."
curl -X GET "$API_URL/api/v1/admin/brands/assets/settings" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -s | jq '.'
echo ""

echo "=== Tests Complete ==="
```

## Expected Audit Log Entries

Check `BackofficeAudits` table after running tests:

```sql
SELECT 
  "Action",
  "TargetType",
  "Meta",
  "CreatedAt"
FROM "BackofficeAudits"
WHERE "TargetType" = 'BrandSettings'
ORDER BY "CreatedAt" DESC;
```

Expected actions:
- `BRAND_ASSETS_INIT`
- `BRAND_MEDIA_UPLOAD` (logo)
- `BRAND_MEDIA_UPLOAD` (favicon)
- `BRAND_BANNER_UPLOAD` (multiple)
- `BRAND_COLORS_UPDATE`
- `BRAND_CONFIG_PUBLISH`

## Verify S3 Files

```bash
# List all files for brand
aws s3 ls s3://brand-assets-prod/assets/bet30/ --recursive

# Expected output:
assets/bet30/banners/home/abc-123.jpg
assets/bet30/banners/home/def-456.jpg
assets/bet30/banners/slots/ghi-789.jpg
assets/bet30/banners/media/logo.png
assets/bet30/banners/media/favicon.ico
assets/bet30/config/config.js
```

## Troubleshooting

### If tests fail with 500 errors:
1. Check application logs
2. Verify AWS credentials
3. Confirm S3 bucket exists and is accessible
4. Check database migration was applied

### If files don't upload:
1. Check IAM permissions
2. Verify bucket policy
3. Check file size and format
4. Review application logs

### If config.js is not accessible:
1. Verify bucket policy allows public read
2. Check CORS configuration
3. Confirm file was uploaded successfully
