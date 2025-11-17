# Brand Assets System - Quick Setup Guide

## Prerequisites

1. AWS Account with S3 access
2. S3 Bucket created: `brand-assets-prod`
3. AWS IAM credentials with S3 permissions

## Step 1: Configure AWS Credentials

### Option A: Environment Variables

```bash
export AWS__AccessKey="YOUR_ACCESS_KEY"
export AWS__SecretKey="YOUR_SECRET_KEY"
```

### Option B: appsettings.json

```json
{
  "AWS": {
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "S3": {
      "BucketName": "brand-assets-prod",
      "Region": "us-east-1"
    }
  }
}
```

### Option C: AWS IAM Role (Production Recommended)

If running on AWS (EC2, ECS, Lambda), use IAM roles. No credentials needed in config.

## Step 2: Create S3 Bucket

```bash
aws s3 mb s3://brand-assets-prod --region us-east-1
```

### Configure Bucket CORS

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

Apply CORS:
```bash
aws s3api put-bucket-cors --bucket brand-assets-prod --cors-configuration file://cors.json
```

### Configure Bucket Policy (Public Read)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
   "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::brand-assets-prod/*"
    }
  ]
}
```

Apply policy:
```bash
aws s3api put-bucket-policy --bucket brand-assets-prod --policy file://bucket-policy.json
```

## Step 3: Run Database Migration

```bash
# Using psql
psql -h YOUR_HOST -U YOUR_USER -d YOUR_DATABASE -f apps/Casino.Infrastructure/Migrations/005_CreateBrandSettings.sql
```

Or manually execute:
```sql
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

CREATE UNIQUE INDEX "IX_BrandSettings_BrandId" ON "BrandSettings" ("BrandId");
```

## Step 4: Test the System

### 1. Initialize Brand Assets

```bash
curl -X POST https://your-api.com/api/v1/admin/brands/assets/initialize \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com"
```

Expected response:
```json
{
  "success": true,
  "message": "Brand assets initialized successfully",
  "foldersCreated": [
    "assets/your-brand/banners/home/",
    "assets/your-brand/banners/slots/",
    "assets/your-brand/banners/live-casino/",
    "assets/your-brand/banners/media/",
    "assets/your-brand/config/"
  ]
}
```

### 2. Upload Logo

```bash
curl -X POST https://your-api.com/api/v1/admin/brands/assets/upload/media/logo \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com" \
  -F "file=@logo.png"
```

### 3. Upload Banner

```bash
curl -X POST https://your-api.com/api/v1/admin/brands/assets/upload/banner/home \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com" \
  -F "file=@banner1.jpg"
```

### 4. Update Colors

```bash
curl -X PUT https://your-api.com/api/v1/admin/brands/assets/colors \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com" \
  -H "Content-Type: application/json" \
  -d '{
    "colors": {
      "--color-primary": "#ffb300",
      "--color-secondary": "#2196f3"
    }
  }'
```

### 5. Publish Config

```bash
curl -X POST https://your-api.com/api/v1/admin/brands/assets/publish-config \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com"
```

### 6. Verify Config.js

```bash
curl https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/your-brand/config/config.js
```

Expected output:
```javascript
// Brand Configuration - Auto-generated
// Brand: Your Brand (your-brand)
// Generated: 2025-01-15 10:30:00 UTC

window.gBrandName = "your-brand";

window.gColors = {
  "--color-primary": "#ffb300",
  "--color-secondary": "#2196f3"
};

window.gHomeBannersDesktop = ["https://..."];
window.gSlotsBannersDesktop = [];
window.gLiveCasinoBannersDesktop = [];

window.gLogo = "https://...";
window.gFavicon = "";
```

## Step 5: Integrate Frontend

Add to your HTML:

```html
<!DOCTYPE html>
<html>
<head>
    <script src="https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/your-brand/config/config.js"></script>
    <style>
        :root {
    /* Colors will be set by JavaScript */
        }
    </style>
</head>
<body>
    <img id="logo" src="" alt="Logo">
    <div id="banner-carousel"></div>

    <script>
        // Apply colors
        Object.keys(window.gColors).forEach(key => {
        document.documentElement.style.setProperty(key, window.gColors[key]);
 });

        // Set logo
        document.getElementById('logo').src = window.gLogo;

        // Load banners
        const carousel = document.getElementById('banner-carousel');
window.gHomeBannersDesktop.forEach(url => {
            const img = document.createElement('img');
       img.src = url;
          carousel.appendChild(img);
        });
    </script>
</body>
</html>
```

## IAM Policy for S3 Access

Minimum required permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:PutObjectAcl",
        "s3:GetObject",
 "s3:DeleteObject",
        "s3:ListBucket"
 ],
      "Resource": [
        "arn:aws:s3:::brand-assets-prod",
    "arn:aws:s3:::brand-assets-prod/*"
      ]
    }
  ]
}
```

## Troubleshooting

### Error: "Access Denied"

Check:
1. AWS credentials are correct
2. IAM user/role has S3 permissions
3. Bucket policy allows public read

### Error: "Bucket not found"

Check:
1. Bucket name in appsettings.json matches actual bucket
2. Region is correct
3. Bucket exists in your AWS account

### Error: "CORS policy blocked"

Check:
1. CORS configuration is applied to S3 bucket
2. Origin is in allowed origins list

### Images not loading in frontend

Check:
1. Bucket policy allows public read
2. Files have public-read ACL
3. URLs are correct

## Production Checklist

- [ ] AWS IAM role configured (not access keys)
- [ ] S3 bucket CORS configured
- [ ] S3 bucket policy allows public read
- [ ] CloudFront CDN configured (optional but recommended)
- [ ] Database migration applied
- [ ] Backup strategy in place
- [ ] Monitoring and logging configured
- [ ] Cost alerts set up

## Next Steps

1. Configure CloudFront for better performance and caching
2. Set up automated backups
3. Implement image optimization pipeline
4. Create admin panel for easier asset management
5. Add analytics to track asset usage

## Support

For issues or questions:
- Review full documentation: `docs/BRAND-ASSETS-DOCUMENTATION.md`
- Check AWS S3 logs
- Review application logs
- Verify database records in `BrandSettings` table
