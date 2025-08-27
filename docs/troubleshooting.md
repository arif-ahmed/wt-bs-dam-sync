# Troubleshooting and FAQ

This document provides solutions to common issues and frequently asked questions about Brandshare DAM Sync.

## Quick Diagnostic Commands

### Check Service Status

```bash
# Windows
sc query \"BrandShare DAM Sync\"
Get-Service -Name \"BrandShareDAMSync\"

# Linux
sudo systemctl status brandshare-dam-sync
sudo systemctl is-active brandshare-dam-sync

# Docker
docker ps | grep dam-sync
docker logs brandshare-dam-sync
```

### Check Connectivity

```bash
# Test DAM API connectivity
curl -v https://your-dam-instance.com/api/health

# Test with authentication
curl -H \"Authorization: Bearer your-api-key\" \\n     https://your-dam-instance.com/FileSyncMachine/TestConnection

# Check DNS resolution
nslookup your-dam-instance.com
```

### Check Logs

```bash
# Windows Event Logs
Get-EventLog -LogName Application -Source \"BrandShareDAMSync\" -Newest 20

# Linux System Logs
sudo journalctl -u brandshare-dam-sync -n 50
tail -f /var/log/dam-sync/dam-sync.log

# Docker Logs
docker logs --tail 50 brandshare-dam-sync
```

## Common Issues and Solutions

### 1. Service Startup Issues

#### Problem: Service fails to start

**Symptoms:**
- Service shows \"Failed\" status
- Error messages in event log/journal
- Application exits immediately

**Common Causes and Solutions:**

```bash
# Check configuration file syntax
dotnet run --project src/BrandShareDAMSync.Daemon --dry-run

# Verify file permissions
# Windows
icacls \"C:\\BrandShareDAMSync\" /T

# Linux
ls -la /opt/brandshare-dam-sync/
sudo chown -R dam-sync:dam-sync /var/lib/dam-sync

# Check dependencies
ldd /opt/brandshare-dam-sync/BrandShareDAMSyncd  # Linux
```

**Error: \"Permission denied\"**
```bash
# Windows - Grant service account permissions
icacls \"C:\\BrandShareDAMSync\" /grant \"NT SERVICE\\BrandShareDAMSync:(OI)(CI)F\" /T

# Linux - Fix ownership and permissions
sudo chown -R dam-sync:dam-sync /opt/brandshare-dam-sync
sudo chmod +x /opt/brandshare-dam-sync/BrandShareDAMSyncd
```

**Error: \"Configuration is invalid\"**
```bash
# Validate JSON configuration
jq . < appsettings.json

# Check for missing required settings
grep -E \"BaseUrl|ApiKey\" appsettings.json
```

### 2. Database Issues

#### Problem: SQLite database errors

**Error: \"Database is locked\"**
```bash
# Check for other processes using the database
lsof /var/lib/dam-sync/dam-sync.db  # Linux

# Stop service and restart
sudo systemctl stop brandshare-dam-sync
sudo systemctl start brandshare-dam-sync

# Check database integrity
sqlite3 /var/lib/dam-sync/dam-sync.db \"PRAGMA integrity_check;\"
```

**Error: \"Database disk image is malformed\"**
```bash
# Backup current database
cp /var/lib/dam-sync/dam-sync.db /var/lib/dam-sync/dam-sync.db.corrupt

# Attempt repair
sqlite3 /var/lib/dam-sync/dam-sync.db.corrupt \".dump\" | \\nsqlite3 /var/lib/dam-sync/dam-sync.db.repaired

# Replace with repaired version
mv /var/lib/dam-sync/dam-sync.db.repaired /var/lib/dam-sync/dam-sync.db
```

**Error: \"No such table: Tenants\"**
```bash
# Run database migrations
dotnet ef database update --project src/BrandShareDAMSync.Infrastructure.Persistence

# Or use migration script
./scripts/migrate.sh
```

### 3. Network and API Issues

#### Problem: Authentication failures

**Error: \"401 Unauthorized\"**
```bash
# Verify API key format
echo \"API Key length: $(echo $BRANDSHAREDAM__APIKEY | wc -c)\"

# Test API key manually
curl -H \"Authorization: Bearer $BRANDSHAREDAM__APIKEY\" \\n     https://your-dam-instance.com/FileSyncMachine/TestConnection

# Check tenant configuration
dotnet run --project src/BrandShareDAMSync.Cli -- tenants list
```

**Error: \"SSL/TLS certificate errors\"**
```bash
# Check certificate validity
openssl s_client -connect your-dam-instance.com:443 -servername your-dam-instance.com

# For testing only - bypass SSL validation (NOT for production)
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
```

**Error: \"Connection timeout\"**
```bash
# Test network connectivity
telnet your-dam-instance.com 443

# Check firewall rules
sudo ufw status  # Linux
netsh advfirewall show allprofiles  # Windows

# Increase timeout in configuration
{
  \"BrandShareDam\": {
    \"RequestTimeout\": \"00:05:00\"
  }
}
```

### 4. Sync Operation Issues

#### Problem: Files not syncing

**Check sync job status:**
```bash
# View active jobs
dotnet run --project src/BrandShareDAMSync.Cli -- jobs list

# Check job details
grep \"Job.*started\\|Job.*completed\" /var/log/dam-sync/dam-sync.log
```

**Error: \"File access denied\"**
```bash
# Check file permissions
ls -la /path/to/sync/directory

# Windows - Grant permissions to sync directory
icacls \"C:\\Sync\" /grant \"Everyone:(OI)(CI)F\" /T

# Linux - Fix ownership
sudo chown -R dam-sync:dam-sync /var/sync/directory
```

**Error: \"Insufficient disk space\"**
```bash
# Check available space
df -h /var/lib/dam-sync

# Clean up old files
find /var/lib/dam-sync -name \"*.tmp\" -mtime +1 -delete
```

### 5. Performance Issues

#### Problem: Slow sync operations

**High CPU usage:**
```bash
# Check process resources
top -p $(pgrep BrandShareDAMSyncd)

# Reduce concurrency in configuration
{
  \"Performance\": {
    \"DownloadConcurrency\": 2,
    \"UploadConcurrency\": 1
  }
}
```

**Memory issues:**
```bash
# Monitor memory usage
ps aux | grep BrandShareDAMSyncd

# Check for memory leaks
valgrind --tool=massif ./BrandShareDAMSyncd  # Linux development
```

**Network bottlenecks:**
```bash
# Monitor network usage
iftop  # Linux
netstat -e  # Windows

# Adjust chunk sizes
{
  \"Performance\": {
    \"ChunkSizeBytes\": 4194304,
    \"BufferSizeBytes\": 2048
  }
}
```

## Diagnostic Tools

### Log Analysis Scripts

```bash
#!/bin/bash
# analyze-logs.sh - Comprehensive log analysis

LOG_FILE=\"/var/log/dam-sync/dam-sync.log\"

echo \"=== Error Summary ===\"
grep -E \"ERROR|FATAL\" $LOG_FILE | tail -10

echo -e \"\n=== Sync Job Stats ===\"
grep \"Job.*completed\" $LOG_FILE | \\n  awk '{print $1, $2}' | sort | uniq -c | tail -10

echo -e \"\n=== Performance Metrics ===\"
grep \"Duration:\" $LOG_FILE | \\n  awk '{print $NF}' | \\n  awk '{sum+=$1; count++} END {print \"Average:\", sum/count \"s\", \"Count:\", count}'

echo -e \"\n=== Recent Restarts ===\"
grep -E \"Starting|Stopping\" $LOG_FILE | tail -5
```

### Health Check Script

```bash
#!/bin/bash
# health-check.sh - System health verification

ERRORS=0

echo \"=== Service Status ===\"
if systemctl is-active --quiet brandshare-dam-sync; then
    echo \"✓ Service is running\"
else
    echo \"✗ Service is not running\"
    ((ERRORS++))
fi

echo -e \"\n=== Database Check ===\"
if sqlite3 /var/lib/dam-sync/dam-sync.db \"SELECT 1\" >/dev/null 2>&1; then
    echo \"✓ Database accessible\"
else
    echo \"✗ Database error\"
    ((ERRORS++))
fi

echo -e \"\n=== Disk Space ===\"
USAGE=$(df /var/lib/dam-sync | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $USAGE -lt 90 ]; then
    echo \"✓ Disk space OK ($USAGE% used)\"
else
    echo \"✗ Disk space critical ($USAGE% used)\"
    ((ERRORS++))
fi

echo -e \"\n=== API Connectivity ===\"
if curl -s -f http://localhost:8080/health >/dev/null; then
    echo \"✓ Health endpoint accessible\"
else
    echo \"✗ Health endpoint not responding\"
    ((ERRORS++))
fi

echo -e \"\n=== Summary ===\"
if [ $ERRORS -eq 0 ]; then
    echo \"✓ All checks passed\"
    exit 0
else
    echo \"✗ $ERRORS issues found\"
    exit 1
fi
```

## Frequently Asked Questions

### General Questions

**Q: How do I check if the sync is working?**

A: Monitor the logs and check the health endpoint:
```bash
# Check logs for sync activity
grep \"Job.*completed\" /var/log/dam-sync/dam-sync.log

# Check health endpoint
curl http://localhost:8080/health

# View recent sync statistics
dotnet run --project src/BrandShareDAMSync.Cli -- stats
```

**Q: How often does the sync run?**

A: The sync interval is configurable. Check your settings:
```bash
# View current interval
grep \"IntervalSeconds\" appsettings.json

# Default is 300 seconds (5 minutes)
```

**Q: Can I run multiple sync jobs simultaneously?**

A: Yes, configure the maximum concurrent jobs:
```json
{
  \"Worker\": {
    \"MaxConcurrentJobs\": 3
  }
}
```

### Configuration Questions

**Q: How do I add a new tenant?**

A: Use the CLI to add tenants:
```bash
dotnet run --project src/BrandShareDAMSync.Cli -- tenants add \\n  --base-url \"https://new-tenant.com\" \\n  --api-key \"new-api-key\"
```

**Q: How do I change the sync direction?**

A: Sync direction is set per job. Available options:
- `L2D` - Local to DAM only
- `D2L` - DAM to Local only
- `Both` - Bi-directional sync
- `L2DD` - Local to DAM with delete
- `D2LD` - DAM to Local with delete

**Q: Where are the configuration files located?**

A: Configuration file locations:
- **Development**: `src/BrandShareDAMSync.Daemon/appsettings.json`
- **Windows Service**: `C:\\BrandShareDAMSync\\appsettings.json`
- **Linux Service**: `/opt/brandshare-dam-sync/appsettings.json`
- **Docker**: Mounted volume or environment variables

### Performance Questions

**Q: The sync is very slow. How can I improve performance?**

A: Several optimization options:
```json
{
  \"Performance\": {
    \"DownloadConcurrency\": 10,
    \"UploadConcurrency\": 5,
    \"ChunkSizeBytes\": 8388608
  },
  \"Worker\": {
    \"MaxConcurrentJobs\": 5
  }
}
```

**Q: How much disk space does the application need?**

A: Space requirements:
- **Application**: ~50 MB
- **Database**: 10-100 MB (depends on file count)
- **Logs**: 10-50 MB (with rotation)
- **Temp files**: Variable (cleaned automatically)
- **Sync data**: Depends on your files

**Q: Can I limit bandwidth usage?**

A: While there's no built-in bandwidth limiting, you can:
- Reduce concurrency settings
- Use smaller chunk sizes
- Implement OS-level traffic shaping

### Security Questions

**Q: How are API keys stored securely?**

A: API keys can be stored:
- Environment variables (recommended for production)
- User secrets (development)
- Encrypted configuration sections
- Azure Key Vault (enterprise)

**Q: What network ports need to be open?**

A: Required outbound connections:
- HTTPS (443) to DAM instance
- HTTPS (443) to S3 storage
- Optional: Health check port (8080) for monitoring

### Troubleshooting Questions

**Q: The service keeps restarting. What should I check?**

A: Common causes:
```bash
# Check for configuration errors
journalctl -u brandshare-dam-sync -n 50

# Verify file permissions
ls -la /opt/brandshare-dam-sync/

# Check database accessibility
sqlite3 /var/lib/dam-sync/dam-sync.db \"SELECT 1\"

# Monitor resource usage
top -p $(pgrep BrandShareDAMSyncd)
```

**Q: Files are not being synchronized. What could be wrong?**

A: Debugging steps:
1. Check service status
2. Verify API connectivity
3. Confirm job configuration
4. Check file permissions
5. Review sync logs
6. Test with small files first

**Q: How do I reset the sync state?**

A: To reset sync state:
```bash
# Stop service
sudo systemctl stop brandshare-dam-sync

# Clear sync metadata (optional - will re-sync everything)
sqlite3 /var/lib/dam-sync/dam-sync.db \"DELETE FROM Files; DELETE FROM Folders;\"

# Restart service
sudo systemctl start brandshare-dam-sync
```

## Getting Support

### Before Contacting Support

1. **Gather diagnostic information**:
   ```bash
   # Run health check
   ./health-check.sh
   
   # Collect logs
   tar -czf support-logs.tar.gz /var/log/dam-sync/
   
   # Export configuration (sanitize sensitive data)
   dotnet run --project src/BrandShareDAMSync.Cli -- config export
   ```

2. **Document the issue**:
   - What were you trying to do?
   - What happened instead?
   - When did the issue start?
   - Any recent changes?

3. **Check known issues**:
   - Review this troubleshooting guide
   - Check project GitHub issues
   - Search community forums

### Support Resources

- **Documentation**: `docs/` directory
- **GitHub Issues**: Create detailed bug reports
- **Health Checks**: `curl http://localhost:8080/health`
- **Log Analysis**: Use provided diagnostic scripts

This troubleshooting guide covers the most common issues and their solutions. For persistent problems, gather diagnostic information using the provided scripts before seeking support.