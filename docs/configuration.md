# Configuration Documentation

This document provides comprehensive guidance on configuring the Brandshare DAM Sync system, including tenant setup, job configuration, security settings, and environment-specific configurations.

## Overview

Brandshare DAM Sync uses a multi-layered configuration approach:

1. **Application Settings** - `appsettings.json` files for general configuration
2. **Tenant Configuration** - Database-stored tenant-specific settings
3. **Job Configuration** - Sync job definitions with strategies and schedules
4. **Environment Variables** - Runtime and deployment-specific overrides
5. **User Secrets** - Development-time sensitive configuration

## Application Settings

### Base Configuration (`appsettings.json`)

```json
{
  \"ConnectionStrings\": {
    \"DefaultConnection\": \"Data Source=dam-sync.db\"
  },
  \"BrandShareDam\": {
    \"BaseUrl\": \"https://your-dam-instance.com\",
    \"ApiKey\": \"\",
    \"RequestTimeout\": \"00:02:00\",
    \"MaxRetryAttempts\": 3,
    \"CircuitBreakerThreshold\": 5,
    \"CircuitBreakerDuration\": \"00:00:30\"
  },
  \"Worker\": {
    \"IntervalSeconds\": 300,
    \"MaxConcurrentJobs\": 3,
    \"JobTimeoutMinutes\": 60,
    \"EnableHealthChecks\": true
  },
  \"Logging\": {
    \"LogLevel\": {
      \"Default\": \"Information\",
      \"Microsoft\": \"Warning\",
      \"Microsoft.EntityFrameworkCore\": \"Warning\",
      \"BrandshareDamSync\": \"Debug\"
    },
    \"Console\": {
      \"TimestampFormat\": \"yyyy-MM-dd HH:mm:ss \"
    },
    \"File\": {
      \"Path\": \"logs/dam-sync.log\",
      \"RetentionDays\": 30,
      \"MaxFileSizeMB\": 10
    }
  },
  \"Performance\": {
    \"DownloadConcurrency\": 5,
    \"UploadConcurrency\": 3,
    \"ChunkSizeBytes\": 8388608,
    \"BufferSizeBytes\": 4096
  }
}
```

### Environment-Specific Overrides

#### Development (`appsettings.Development.json`)

```json
{
  \"BrandShareDam\": {
    \"BaseUrl\": \"https://dev-dam.company.com\"
  },
  \"Worker\": {
    \"IntervalSeconds\": 60,
    \"MaxConcurrentJobs\": 1
  },
  \"Logging\": {
    \"LogLevel\": {
      \"Default\": \"Debug\",
      \"BrandshareDamSync\": \"Trace\"
    }
  },
  \"Performance\": {
    \"DownloadConcurrency\": 2,
    \"UploadConcurrency\": 1
  }
}
```

#### Production (`appsettings.Production.json`)

```json
{
  \"BrandShareDam\": {
    \"RequestTimeout\": \"00:05:00\",
    \"MaxRetryAttempts\": 5
  },
  \"Worker\": {
    \"IntervalSeconds\": 300,
    \"MaxConcurrentJobs\": 5,
    \"EnableHealthChecks\": true
  },
  \"Logging\": {
    \"LogLevel\": {
      \"Default\": \"Information\",
      \"Microsoft\": \"Error\"
    }
  },
  \"Performance\": {
    \"DownloadConcurrency\": 10,
    \"UploadConcurrency\": 5
  }
}
```

## Tenant Configuration

### Adding Tenants via CLI

#### Interactive Mode

```bash
# Start interactive tenant management
dotnet run --project src/BrandShareDAMSync.Cli

# Follow the prompts to:
# 1. Add new tenant
# 2. Configure base URL and API key
# 3. Set optional domain
# 4. Test connection
```

#### Command Line Mode

```bash
# Add a new tenant
dotnet run --project src/BrandShareDAMSync.Cli -- tenants add \\n  --base-url \"https://company-dam.marcombox.com\" \\n  --api-key \"ak...\" \\n  --domain \"company.com\"

# List all tenants
dotnet run --project src/BrandShareDAMSync.Cli -- tenants list

# Update tenant configuration
dotnet run --project src/BrandShareDAMSync.Cli -- tenants update \\n  --id \"tenant-123\" \\n  --base-url \"https://new-url.com\"

# Delete tenant
dotnet run --project src/BrandShareDAMSync.Cli -- tenants delete \\n  --id \"tenant-123\"
```

### Tenant Configuration Properties

```csharp
public class Tenant
{
    public string Id { get; set; }              // Unique identifier
    public string BaseUrl { get; set; }         // DAM instance URL
    public string ApiKey { get; set; }          // Authentication key
    public string? Domain { get; set; }         // Optional domain
    public bool IsActive { get; set; }          // Enable/disable
    public DateTime CreatedAt { get; set; }     // Creation timestamp
    public DateTime LastModified { get; set; }  // Last update
}
```

### Multiple Tenant Example

```json
{
  \"tenants\": [
    {
      \"id\": \"company-prod\",
      \"baseUrl\": \"https://prod-dam.company.com\",
      \"apiKey\": \"ak_prod_key_here\",
      \"domain\": \"company.com\",
      \"isActive\": true
    },
    {
      \"id\": \"company-staging\",
      \"baseUrl\": \"https://staging-dam.company.com\",
      \"apiKey\": \"ak_staging_key_here\",
      \"domain\": \"staging.company.com\",
      \"isActive\": false
    }
  ]
}
```

## Job Configuration

### Sync Job Properties

```csharp
public class SyncJob
{
    public string Id { get; set; }
    public string JobName { get; set; }            // Display name
    public string VolumeName { get; set; }         // DAM volume identifier
    public string VolumePath { get; set; }         // DAM volume path
    public string VolumeId { get; set; }           // DAM volume ID
    public string DestinationPath { get; set; }    // Local directory path
    public int? JobIntervalMinutes { get; set; }   // Sync frequency
    public SyncDirection SyncDirection { get; set; } // Sync direction
    public string JobStatus { get; set; }          // Current status
    public bool IsActive { get; set; }             // Enable/disable
    public string PrimaryLocation { get; set; }    // Primary data location
    public SyncJobStatus SyncJobStatus { get; set; } // Detailed status
    public string? LastItemId { get; set; }        // Pagination cursor
    public long LastRunTime { get; set; }          // Last execution time
    public string TenantId { get; set; }           // Associated tenant
}
```

### Sync Directions

```csharp
public enum SyncDirection
{
    L2D = 0,        // Local to DAM only
    D2L = 1,        // DAM to Local only
    L2DD = 2,       // Local to DAM with delete
    D2LD = 3,       // DAM to Local with delete
    Both = 4,       // Bi-directional sync
    Unknown = 99    // Error state
}
```

### Job Configuration Examples

#### One-Way Upload Job

```json
{
  \"jobName\": \"Marketing Assets Upload\",
  \"volumeName\": \"Marketing\",
  \"volumePath\": \"/marketing\",
  \"destinationPath\": \"C:\\\\Marketing\\\\Assets\",
  \"jobIntervalMinutes\": 30,
  \"syncDirection\": \"L2D\",
  \"isActive\": true,
  \"primaryLocation\": \"Local\"
}
```

#### One-Way Download Job

```json
{
  \"jobName\": \"Product Images Download\",
  \"volumeName\": \"Products\",
  \"volumePath\": \"/products/images\",
  \"destinationPath\": \"/var/www/html/images/products\",
  \"jobIntervalMinutes\": 60,
  \"syncDirection\": \"D2L\",
  \"isActive\": true,
  \"primaryLocation\": \"DAM\"
}
```

#### Bi-Directional Sync Job

```json
{
  \"jobName\": \"Design Team Sync\",
  \"volumeName\": \"Design\",
  \"volumePath\": \"/design/working\",
  \"destinationPath\": \"C:\\\\Design\\\\Working\",
  \"jobIntervalMinutes\": 15,
  \"syncDirection\": \"Both\",
  \"isActive\": true,
  \"primaryLocation\": \"Both\"
}
```

## Security Configuration

### API Key Management

#### Development (User Secrets)

```bash
# Initialize user secrets
dotnet user-secrets init --project src/BrandShareDAMSync.Daemon

# Set API key
dotnet user-secrets set \"BrandShareDam:ApiKey\" \"your-api-key\" \\n  --project src/BrandShareDAMSync.Daemon

# Set tenant-specific keys
dotnet user-secrets set \"Tenants:0:ApiKey\" \"tenant-1-key\" \\n  --project src/BrandShareDAMSync.Daemon
```

#### Production (Environment Variables)

```bash
# Windows
set BRANDSHAREDAM__APIKEY=your-production-key
set TENANTS__0__APIKEY=tenant-production-key

# Linux/macOS
export BRANDSHAREDAM__APIKEY=\"your-production-key\"
export TENANTS__0__APIKEY=\"tenant-production-key\"
```

#### Azure Key Vault (Enterprise)

```json
{
  \"AzureKeyVault\": {
    \"Enabled\": true,
    \"VaultUrl\": \"https://your-vault.vault.azure.net/\",
    \"ClientId\": \"client-id\",
    \"ClientSecret\": \"client-secret\",
    \"TenantId\": \"azure-tenant-id\"
  }
}
```

### File System Security

#### Directory Permissions

```bash
# Windows - Grant service account permissions
icacls \"C:\\Sync\" /grant \"SERVICE_ACCOUNT:(OI)(CI)F\" /T

# Linux - Set appropriate ownership and permissions
sudo chown -R dam-sync:dam-sync /var/lib/dam-sync
sudo chmod -R 755 /var/lib/dam-sync
sudo chmod -R 644 /var/lib/dam-sync/*.db
```

#### Service Account Configuration

```xml
<!-- Windows Service Configuration -->
<service>
  <name>BrandShare DAM Sync</name>
  <account>NT SERVICE\\BrandShareDAMSync</account>
  <privileges>
    <privilege>SeServiceLogonRight</privilege>
    <privilege>SeCreateGlobalPrivilege</privilege>
  </privileges>
</service>
```

## Database Configuration

### SQLite Configuration

#### Connection Strings

```json
{
  \"ConnectionStrings\": {
    \"DefaultConnection\": \"Data Source=dam-sync.db;Cache=Shared;Journal Mode=WAL;\",
    \"Development\": \"Data Source=dev-dam-sync.db;Cache=Shared;\",
    \"Testing\": \"Data Source=:memory:\"
  }
}
```

#### Database Path Configuration

```csharp
public static class DatabasePath
{
    public static string GetDbPath(IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            // Development: Use local directory
            return Path.Combine(Directory.GetCurrentDirectory(), \"dam-sync-dev.db\");
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Use ProgramData
            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(path, \"BrandShareDAMSync\", \"dam-sync.db\");
        }
        
        // Linux/macOS: Use standard locations
        return \"/var/lib/dam-sync/dam-sync.db\";
    }
}
```

### Database Migrations

#### Manual Migration

```bash
# Windows
.\\scripts\\migrate.ps1

# Linux/macOS
./scripts/migrate.sh

# Alternative: Direct dotnet command
dotnet ef database update --project src/BrandShareDAMSync.Infrastructure.Persistence
```

#### Automatic Migration (Production)

```csharp
// In Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DamSyncDbContext>();
    await db.Database.MigrateAsync();
    
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}
```

## Performance Configuration

### Concurrency Settings

```json
{
  \"Performance\": {
    \"DownloadConcurrency\": 5,
    \"UploadConcurrency\": 3,
    \"MaxParallelJobs\": 3,
    \"ChunkSizeBytes\": 8388608,
    \"BufferSizeBytes\": 4096,
    \"ConnectionPoolSize\": 10,
    \"HttpClientTimeout\": \"00:02:00\"
  }
}
```

### Memory Management

```json
{
  \"Memory\": {
    \"MaxCacheSize\": \"256MB\",
    \"EnableGCOptimization\": true,
    \"LargeObjectHeapCompaction\": true
  }
}
```

### Throttling Configuration

```json
{
  \"Throttling\": {
    \"RequestsPerSecond\": 10,
    \"RequestsPerMinute\": 600,
    \"BurstAllowance\": 50,
    \"CooldownPeriod\": \"00:01:00\"
  }
}
```

## Monitoring and Observability

### Health Checks

```json
{
  \"HealthChecks\": {
    \"Enabled\": true,
    \"Port\": 8080,
    \"Path\": \"/health\",
    \"Checks\": {
      \"Database\": true,
      \"DAMConnectivity\": true,
      \"DiskSpace\": true,
      \"Memory\": true
    },
    \"Thresholds\": {
      \"DiskSpaceGB\": 5,
      \"MemoryUsagePercent\": 80
    }
  }
}
```

### Application Insights (Optional)

```json
{
  \"ApplicationInsights\": {
    \"InstrumentationKey\": \"your-key-here\",
    \"EnableAdaptiveSampling\": true,
    \"SamplingRate\": 0.1,
    \"EnablePerformanceCounters\": true
  }
}
```

### Custom Metrics

```json
{
  \"Metrics\": {
    \"Enabled\": true,
    \"ExportInterval\": \"00:01:00\",
    \"Exporters\": [
      {
        \"Type\": \"Console\",
        \"Enabled\": false
      },
      {
        \"Type\": \"Prometheus\",
        \"Enabled\": true,
        \"Port\": 9090,
        \"Path\": \"/metrics\"
      }
    ]
  }
}
```

## Environment-Specific Configuration

### Docker Configuration

```dockerfile
# Environment variables for Docker
ENV ASPNETCORE_ENVIRONMENT=Production
ENV BRANDSHAREDAM__BASEURL=https://dam.company.com
ENV WORKER__INTERVALSECONDS=300
ENV LOGGING__LOGLEVEL__DEFAULT=Information

# Volume mounts for configuration
VOLUME [\"/app/config\", \"/app/data\", \"/app/logs\"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  dam-sync:
    image: brandshare-dam-sync:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - BRANDSHAREDAM__BASEURL=https://dam.company.com
      - WORKER__INTERVALSECONDS=300
    volumes:
      - ./config:/app/config:ro
      - ./data:/app/data
      - ./logs:/app/logs
    restart: unless-stopped
```

### Kubernetes Configuration

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: dam-sync-config
data:
  appsettings.json: |
    {
      \"Worker\": {
        \"IntervalSeconds\": 300
      },
      \"Logging\": {
        \"LogLevel\": {
          \"Default\": \"Information\"
        }
      }
    }
---
apiVersion: v1
kind: Secret
metadata:
  name: dam-sync-secrets
type: Opaque
data:
  api-key: <base64-encoded-api-key>
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dam-sync
spec:
  replicas: 1
  selector:
    matchLabels:
      app: dam-sync
  template:
    metadata:
      labels:
        app: dam-sync
    spec:
      containers:
      - name: dam-sync
        image: brandshare-dam-sync:latest
        env:
        - name: BRANDSHAREDAM__APIKEY
          valueFrom:
            secretKeyRef:
              name: dam-sync-secrets
              key: api-key
        volumeMounts:
        - name: config
          mountPath: /app/config
        - name: data
          mountPath: /app/data
      volumes:
      - name: config
        configMap:
          name: dam-sync-config
      - name: data
        persistentVolumeClaim:
          claimName: dam-sync-data
```

## Troubleshooting Configuration

### Common Configuration Issues

1. **Invalid API Key**
   ```json
   {
     \"error\": \"Unauthorized\",
     \"solution\": \"Verify API key in tenant configuration\"
   }
   ```

2. **Database Connection Issues**
   ```json
   {
     \"error\": \"SQLite Error: database is locked\",
     \"solution\": \"Check file permissions and ensure no other processes are using the database\"
   }
   ```

3. **Network Connectivity**
   ```json
   {
     \"error\": \"Request timeout\",
     \"solution\": \"Check network connectivity and increase timeout values\"
   }
   ```

### Configuration Validation

```csharp
public class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration config)
    {
        // Required settings validation
        var baseUrl = config[\"BrandShareDam:BaseUrl\"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ConfigurationException(\"BrandShareDam:BaseUrl is required\");
        }
        
        // URL format validation
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ConfigurationException(\"BrandShareDam:BaseUrl must be a valid URL\");
        }
        
        // Numeric range validation
        var intervalSeconds = config.GetValue<int>(\"Worker:IntervalSeconds\");
        if (intervalSeconds < 10 || intervalSeconds > 86400)
        {
            throw new ConfigurationException(\"Worker:IntervalSeconds must be between 10 and 86400\");
        }
    }
}
```

### Configuration Testing

```bash
# Test configuration validity
dotnet run --project src/BrandShareDAMSync.Cli -- config validate

# Test DAM connectivity
dotnet run --project src/BrandShareDAMSync.Cli -- tenants test --id \"tenant-123\"

# Export current configuration
dotnet run --project src/BrandShareDAMSync.Cli -- config export --output config-backup.json
```

This configuration documentation provides comprehensive guidance for setting up and managing Brandshare DAM Sync in various environments, from development to production deployments.