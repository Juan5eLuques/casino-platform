# Brand Assets Management System

## Overview

Complete backend implementation for managing brand assets (banners, logos, media, and config.js) using AWS S3 and .NET 9.

## Architecture

### AWS S3 Bucket Structure

**Bucket Name:** `brand-assets-prod`  
**Region:** `us-east-1`

```
assets/
  {BRANDNAME}/
    banners/
      home/
      slots/
      live-casino/
      media/
    config/
      config.js
```

### Database Schema

**Table:** `BrandSettings`

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| BrandId | uuid | Foreign key to Brands table (unique) |
| Colors | jsonb | Color palette JSON |
| Images | jsonb | Banners and media URLs JSON |
| CreatedAt | timestamptz | Creation timestamp |
| UpdatedAt | timestamptz | Last update timestamp |

**Images JSON Structure:**
```json
{
  "banners": {
    "home": ["url1", "url2"],
    "slots": ["url1"],
    "live-casino": []
  },
  "media": {
    "logo": "url",
    "favicon": "url",
    "others": ["url1", "url2"]
  }
}
```

**Colors JSON Structure:**
```json
{
  "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3",
  "--color-accent": "#e91e63"
}
```

## API Endpoints

All endpoints require authentication and are scoped to the brand resolved by host.

### Base URL
```
/api/v1/admin/brands/assets
```

### 1. Initialize Brand Assets

**POST** `/initialize`

Creates the complete S3 folder structure for a brand.

**Response:**
```json
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

### 2. Upload Banner

**POST** `/upload/banner/{section}`

Upload a banner image to a specific section.

**Path Parameters:**
- `section`: `home`, `slots`, or `live-casino`

**Body:** `multipart/form-data`
- `file`: Image file (max 5MB, formats: jpg, jpeg, png, gif, webp, svg)

**Validation:**
- Maximum 5 banners per section
- File size limit: 5MB
- Allowed extensions: .jpg, .jpeg, .png, .gif, .webp, .svg

**Response:**
```json
{
  "success": true,
  "url": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/abc123.jpg",
  "section": "home",
  "fileName": "abc123.jpg",
  "totalBannersInSection": 3
}
```

### 3. Upload Media

**POST** `/upload/media/{type}`

Upload media files (logo, favicon, or other).

**Path Parameters:**
- `type`: `logo`, `favicon`, or `other`

**Body:** `multipart/form-data`
- `file`: Image file (max 5MB)

**Behavior:**
- For `logo` and `favicon`: Replaces existing file
- For `other`: Adds to the list

**Response:**
```json
{
  "success": true,
  "url": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/logo.png",
  "type": "logo",
  "fileName": "logo.png"
}
```

### 4. Delete Banner

**DELETE** `/banner/{section}/{fileName}`

Delete a specific banner from a section.

**Path Parameters:**
- `section`: `home`, `slots`, or `live-casino`
- `fileName`: Name of the file to delete

**Response:**
```json
{
  "success": true,
  "message": "Banner deleted successfully"
}
```

### 5. Delete Media

**DELETE** `/media/{type}`

Delete a media file (logo or favicon only).

**Path Parameters:**
- `type`: `logo` or `favicon`

**Response:**
```json
{
  "success": true,
  "message": "Media deleted successfully"
}
```

### 6. Publish Config.js

**POST** `/publish-config`

Generate and publish the config.js file to S3.

**Response:**
```json
{
  "success": true,
  "configUrl": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js",
  "publishedAt": "2025-01-15T10:30:00Z"
}
```

### 7. Get Brand Settings

**GET** `/settings`

Get current brand settings including colors, images, and config URL.

**Response:**
```json
{
  "brandId": "123e4567-e89b-12d3-a456-426614174000",
  "brandName": "Bet30 Casino",
  "brandCode": "bet30",
  "colors": {
    "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3"
  },
  "banners": {
    "home": [
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/banner1.jpg",
      "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/banner2.jpg"
    ],
    "slots": [],
    "liveCasino": []
  },
  "media": {
    "logo": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/logo.png",
    "favicon": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/favicon.ico",
    "others": []
  },
  "configJsUrl": "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js",
  "updatedAt": "2025-01-15T10:30:00Z"
}
```

### 8. Update Colors

**PUT** `/colors`

Update brand color palette.

**Body:**
```json
{
  "colors": {
    "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3",
    "--color-accent": "#e91e63",
    "--color-background": "#121212",
    "--color-text": "#ffffff"
  }
}
```

**Response:** Same as Get Brand Settings

## Config.js Format

The generated config.js file exposes global variables for the frontend:

```javascript
// Brand Configuration - Auto-generated
// Brand: Bet30 Casino (bet30)
// Generated: 2025-01-15 10:30:00 UTC

window.gBrandName = "bet30";

window.gColors = {
  "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3",
  "--color-accent": "#e91e63"
};

window.gHomeBannersDesktop = [
  "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/banner1.jpg",
  "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/home/banner2.jpg"
];

window.gSlotsBannersDesktop = [];
window.gLiveCasinoBannersDesktop = [];

window.gLogo = "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/logo.png";
window.gFavicon = "https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/banners/media/favicon.ico";
```

## Frontend Integration

Include the config.js in your HTML:

```html
<!DOCTYPE html>
<html>
<head>
    <script src="https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js"></script>
</head>
<body>
    <script>
        // Access brand configuration
        console.log(window.gBrandName); // "bet30"
        console.log(window.gColors);    // Color palette object
        console.log(window.gLogo);      // Logo URL
        
        // Apply colors to CSS
        Object.keys(window.gColors).forEach(key => {
            document.documentElement.style.setProperty(key, window.gColors[key]);
        });
      
  // Use banners in your carousel
        window.gHomeBannersDesktop.forEach(url => {
// Add to carousel
        });
    </script>
</body>
</html>
```

## Configuration

### appsettings.json

```json
{
  "AWS": {
    "AccessKey": "YOUR_AWS_ACCESS_KEY",
  "SecretKey": "YOUR_AWS_SECRET_KEY",
    "S3": {
      "BucketName": "brand-assets-prod",
      "Region": "us-east-1"
    }
  }
}
```

### Environment Variables (Alternative)

```bash
AWS__AccessKey=YOUR_AWS_ACCESS_KEY
AWS__SecretKey=YOUR_AWS_SECRET_KEY
AWS__S3__BucketName=brand-assets-prod
AWS__S3__Region=us-east-1
```

**Note:** If AWS credentials are not provided in configuration, the SDK will use the default AWS credentials chain (IAM roles, environment variables, AWS CLI profile, etc.).

## Services

### IS3Service

Low-level S3 operations:
- `InitializeBrandFoldersAsync()` - Create folder structure
- `UploadFileAsync()` - Upload file to S3
- `DeleteFileAsync()` - Delete file from S3
- `FileExistsAsync()` - Check if file exists
- `ListFilesAsync()` - List files in a folder
- `GetPublicUrl()` - Get public URL for a file

### IBrandAssetsService

High-level brand assets management:
- `InitializeBrandAssetsAsync()` - Initialize complete structure
- `UploadBannerAsync()` - Upload banner with validation
- `UploadMediaAsync()` - Upload media with validation
- `DeleteBannerAsync()` - Delete banner and update DB
- `DeleteMediaAsync()` - Delete media and update DB
- `PublishConfigAsync()` - Generate and publish config.js
- `GetBrandSettingsAsync()` - Get current settings
- `UpdateColorsAsync()` - Update color palette

## Workflow

### 1. Create New Brand

When a new brand is created:

```bash
# 1. Create brand via existing endpoint
POST /api/v1/admin/brands

# 2. Initialize assets structure
POST /api/v1/admin/brands/assets/initialize
```

This automatically:
- Creates S3 folder structure
- Creates BrandSettings record in database
- Prepares brand for asset uploads

### 2. Upload Assets

```bash
# Upload logo
POST /api/v1/admin/brands/assets/upload/media/logo
Content-Type: multipart/form-data
file: logo.png

# Upload favicon
POST /api/v1/admin/brands/assets/upload/media/favicon
file: favicon.ico

# Upload home banners
POST /api/v1/admin/brands/assets/upload/banner/home
file: banner1.jpg

POST /api/v1/admin/brands/assets/upload/banner/home
file: banner2.jpg
```

### 3. Configure Colors

```bash
PUT /api/v1/admin/brands/assets/colors
{
  "colors": {
    "--color-primary": "#ffb300",
    "--color-secondary": "#2196f3"
  }
}
```

### 4. Publish Configuration

```bash
POST /api/v1/admin/brands/assets/publish-config
```

This generates the config.js file with all settings and uploads it to S3.

### 5. Frontend Consumes

```html
<script src="https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/bet30/config/config.js"></script>
```

## Security

- All endpoints require JWT authentication
- Brand scope is automatically resolved by host (BrandContext middleware)
- Files are uploaded with public-read ACL
- S3 bucket should be configured with proper CORS settings:

```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["GET", "HEAD"],
 "AllowedOrigins": ["*"],
    "ExposeHeaders": []
  }
]
```

## Audit Trail

All operations are logged to `BackofficeAudits`:
- `BRAND_ASSETS_INIT` - Assets initialized
- `BRAND_BANNER_UPLOAD` - Banner uploaded
- `BRAND_MEDIA_UPLOAD` - Media uploaded
- `BRAND_BANNER_DELETE` - Banner deleted
- `BRAND_MEDIA_DELETE` - Media deleted
- `BRAND_CONFIG_PUBLISH` - Config published
- `BRAND_COLORS_UPDATE` - Colors updated

## Validation Rules

### File Upload
- Max file size: 5MB
- Allowed extensions: .jpg, .jpeg, .png, .gif, .webp, .svg
- Allowed content types: image/jpeg, image/png, image/gif, image/webp, image/svg+xml

### Banner Limits
- Maximum 5 banners per section (home, slots, live-casino)

### Media
- Logo: Single file (replaces existing)
- Favicon: Single file (replaces existing)
- Others: Multiple files allowed

## Error Handling

All endpoints return appropriate HTTP status codes:
- `200 OK` - Success
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Authentication required
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

Error response format:
```json
{
  "error": "Detailed error message"
}
```

## Database Migration

Run the SQL migration to create the BrandSettings table:

```sql
-- apps/Casino.Infrastructure/Migrations/005_CreateBrandSettings.sql
```

Or apply manually:

```bash
psql -h HOST -U USER -d DATABASE -f apps/Casino.Infrastructure/Migrations/005_CreateBrandSettings.sql
```

## Dependencies

### NuGet Packages
- `AWSSDK.S3` (v4.0.11.2) - AWS S3 SDK

### Project References
- `Casino.Domain` - Entities
- `Casino.Application` - Services and DTOs
- `Casino.Infrastructure` - Data access
- `Casino.Api` - Endpoints

## Testing

### Example cURL Commands

```bash
# Initialize assets
curl -X POST https://your-api.com/api/v1/admin/brands/assets/initialize \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: bet30.com"

# Upload banner
curl -X POST https://your-api.com/api/v1/admin/brands/assets/upload/banner/home \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: bet30.com" \
  -F "file=@banner.jpg"

# Get settings
curl -X GET https://your-api.com/api/v1/admin/brands/assets/settings \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Host: bet30.com"
```

## Future Enhancements

- [ ] Image optimization and compression
- [ ] CDN integration (CloudFront)
- [ ] Multiple image sizes (responsive)
- [ ] Banner scheduling and rotation rules
- [ ] A/B testing support
- [ ] Analytics integration
- [ ] Backup and versioning
- [ ] Bulk upload support
- [ ] Image preview in admin panel
- [ ] Drag-and-drop reordering
