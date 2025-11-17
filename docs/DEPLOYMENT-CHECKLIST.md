# Brand Assets System - Deployment Checklist

## ? Pre-Deployment Checklist

### Code & Build
- [x] All files created and compiled successfully
- [x] No compilation errors
- [x] NuGet packages installed (AWSSDK.S3)
- [x] Services registered in DI container
- [x] Endpoints mapped in Program.cs
- [ ] Code reviewed and approved

### Database
- [ ] Backup current database
- [ ] Review migration script: `005_CreateBrandSettings.sql`
- [ ] Test migration on staging environment
- [ ] Apply migration to production
- [ ] Verify `BrandSettings` table created
- [ ] Verify foreign key constraint to `Brands` table

### AWS Configuration
- [ ] AWS account credentials available
- [ ] IAM user/role created with S3 permissions
- [ ] S3 bucket created: `brand-assets-prod`
- [ ] Bucket region set: `us-east-1`
- [ ] Bucket CORS configured
- [ ] Bucket policy for public read configured
- [ ] Test S3 access from application

### Application Configuration
- [ ] `appsettings.json` updated with AWS credentials
- [ ] Environment variables configured (if using)
- [ ] AWS region configured correctly
- [ ] Bucket name matches actual bucket

### Security
- [ ] JWT authentication working
- [ ] Brand context resolution working
- [ ] Authorization policies configured
- [ ] S3 bucket access properly scoped
- [ ] Audit logging enabled

## ?? AWS Setup Steps

### 1. Create IAM User/Role

```bash
# Create IAM policy
aws iam create-policy \
  --policy-name BrandAssetsS3Access \
  --policy-document file://s3-policy.json

# Attach policy to user/role
aws iam attach-user-policy \
  --user-name your-app-user \
  --policy-arn arn:aws:iam::ACCOUNT_ID:policy/BrandAssetsS3Access
```

**s3-policy.json:**
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

### 2. Create S3 Bucket

```bash
# Create bucket
aws s3 mb s3://brand-assets-prod --region us-east-1

# Disable block public access (for public reads)
aws s3api put-public-access-block \
  --bucket brand-assets-prod \
  --public-access-block-configuration \
    "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false"
```

### 3. Configure CORS

**cors.json:**
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

```bash
aws s3api put-bucket-cors \
  --bucket brand-assets-prod \
  --cors-configuration file://cors.json
```

### 4. Set Bucket Policy

**bucket-policy.json:**
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

```bash
aws s3api put-bucket-policy \
  --bucket brand-assets-prod \
  --policy file://bucket-policy.json
```

## ?? Database Deployment

### 1. Backup Current Database

```bash
pg_dump -h HOST -U USER -d DATABASE > backup_$(date +%Y%m%d).sql
```

### 2. Apply Migration

```bash
# Option 1: Using psql
psql -h HOST -U USER -d DATABASE -f apps/Casino.Infrastructure/Migrations/005_CreateBrandSettings.sql

# Option 2: Manual execution
# Copy and paste SQL from migration file into database client
```

### 3. Verify Migration

```sql
-- Check table exists
SELECT table_name 
FROM information_schema.tables 
WHERE table_name = 'BrandSettings';

-- Check columns
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'BrandSettings';

-- Verify foreign key
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name = 'BrandSettings'
  AND tc.constraint_type = 'FOREIGN KEY';
```

## ?? Application Deployment

### 1. Update Configuration

**Production appsettings.json:**
```json
{
  "AWS": {
    "AccessKey": "",  // Leave empty if using IAM role
    "SecretKey": "",  // Leave empty if using IAM role
    "S3": {
      "BucketName": "brand-assets-prod",
  "Region": "us-east-1"
    }
  }
}
```

**Or Environment Variables:**
```bash
export AWS__AccessKey="YOUR_ACCESS_KEY"
export AWS__SecretKey="YOUR_SECRET_KEY"
export AWS__S3__BucketName="brand-assets-prod"
export AWS__S3__Region="us-east-1"
```

### 2. Build and Deploy

```bash
# Build project
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release --output ./publish

# Deploy to server (method depends on hosting)
# - Copy files to server
# - Update IIS/Nginx configuration
# - Restart application
```

### 3. Health Check

```bash
# Check application started
curl https://your-api.com/health

# Check Swagger available
curl https://your-api.com/swagger/index.html
```

## ?? Post-Deployment Testing

### 1. Smoke Tests

```bash
# Set environment
export API_URL="https://your-api.com"
export JWT_TOKEN="your_jwt_token"
export BRAND_HOST="your-brand.com"

# Test 1: Initialize (should work)
curl -X POST "$API_URL/api/v1/admin/brands/assets/initialize" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST"

# Test 2: Upload logo (should work)
curl -X POST "$API_URL/api/v1/admin/brands/assets/upload/media/logo" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST" \
  -F "file=@test-logo.png"

# Test 3: Get settings (should work)
curl -X GET "$API_URL/api/v1/admin/brands/assets/settings" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST"

# Test 4: Publish config (should work)
curl -X POST "$API_URL/api/v1/admin/brands/assets/publish-config" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Host: $BRAND_HOST"

# Test 5: Verify config.js accessible
curl https://brand-assets-prod.s3.us-east-1.amazonaws.com/assets/YOUR_BRAND/config/config.js
```

### 2. Full Test Suite

Run the complete test suite from `BRAND-ASSETS-TEST-EXAMPLES.md`

### 3. Verify Database

```sql
-- Check BrandSettings records created
SELECT COUNT(*) FROM "BrandSettings";

-- Check audit logs
SELECT "Action", COUNT(*) 
FROM "BackofficeAudits"
WHERE "TargetType" = 'BrandSettings'
GROUP BY "Action";
```

### 4. Verify S3

```bash
# List files uploaded
aws s3 ls s3://brand-assets-prod/assets/ --recursive
```

## ?? Monitoring Setup

### 1. Application Logs

Monitor logs for:
- S3 upload/delete operations
- Configuration errors
- Authentication failures
- File validation errors

```bash
# Example log queries (adjust for your logging system)
tail -f /var/log/casino-api/app.log | grep "BrandAssets"
```

### 2. AWS CloudWatch

Set up CloudWatch alarms for:
- S3 bucket size
- S3 request count
- S3 error rate
- Cost alerts

### 3. Database Monitoring

Monitor:
- BrandSettings table size
- Query performance
- Foreign key constraint violations

## ?? Rollback Plan

If issues occur after deployment:

### 1. Application Rollback

```bash
# Restore previous version
# Method depends on deployment strategy
```

### 2. Database Rollback

```sql
-- Drop BrandSettings table
DROP TABLE IF EXISTS "BrandSettings" CASCADE;

-- Restore from backup
psql -h HOST -U USER -d DATABASE < backup_YYYYMMDD.sql
```

### 3. S3 Cleanup (if needed)

```bash
# Delete test files
aws s3 rm s3://brand-assets-prod/assets/test-brand/ --recursive
```

## ? Sign-Off Checklist

- [ ] Database migration successful
- [ ] AWS S3 bucket accessible
- [ ] Application deployed and running
- [ ] All smoke tests passed
- [ ] Full test suite passed
- [ ] Monitoring configured
- [ ] Logs reviewed for errors
- [ ] Documentation reviewed
- [ ] Team trained on new features
- [ ] Rollback plan documented and tested

## ?? Support Contacts

- **AWS Support:** [AWS Support Portal]
- **Database Admin:** [Contact Info]
- **DevOps Team:** [Contact Info]
- **Development Team:** [Contact Info]

## ?? Deployment Notes

**Date:** _______________  
**Deployed By:** _______________  
**Version:** _______________  
**Environment:** ? Staging  ? Production  

**Issues Encountered:**
- None / List issues

**Resolution:**
- N/A / How issues were resolved

**Sign-Off:**
- Developer: _______________
- QA: _______________
- DevOps: _______________
- Product Owner: _______________

## ?? Post-Deployment

- [ ] Announce deployment to team
- [ ] Update documentation
- [ ] Schedule follow-up review (1 week)
- [ ] Monitor for 24 hours
- [ ] Collect feedback from users
- [ ] Plan next improvements

---

**Status:** ? Ready for Deployment  ? Deployed Successfully  ? Issues Found  ? Rolled Back
