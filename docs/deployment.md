# Deployment and Operations Documentation

This document provides guidance for deploying and operating Brandshare DAM Sync in various environments.

## Deployment Options

- **Standalone Executable** - Self-contained binaries
- **Windows Service** - Background service for Windows
- **Linux Systemd Service** - System daemon for Linux
- **Docker Container** - Containerized deployment
- **Kubernetes Cluster** - Scalable orchestration

## System Requirements

**Minimum:**
- CPU: 2 cores, 2.0 GHz
- RAM: 2 GB
- Storage: 1 GB free space
- Network: Internet access to DAM instance

**Recommended:**
- CPU: 4 cores, 2.5 GHz+
- RAM: 4 GB+
- Storage: 10 GB (SSD preferred)

## Building from Source

```bash
# Clone and build
git clone <repository-url>
cd wt-bs-dam-sync
dotnet restore
dotnet build -c Release

# Create deployments
.\\scripts\\publish.ps1  # Windows
./scripts/publish.sh   # Linux/macOS
```

## Windows Deployment

### Windows Service Installation

```powershell
# Copy binaries
Copy-Item \"publish\\win\\worker\\win-x64\\*\" -Destination \"C:\\BrandShareDAMSync\"

# Create service
sc create \"BrandShare DAM Sync\" `
   binPath=\"C:\\BrandShareDAMSync\\BrandShareDAMSyncd.exe\" `
   start=auto

# Start service
sc start \"BrandShare DAM Sync\"
```

### Service Management

```powershell
# Check status
Get-Service -Name \"BrandShareDAMSync\"

# View logs
Get-EventLog -LogName Application -Source \"BrandShareDAMSync\"

# Restart
Restart-Service -Name \"BrandShareDAMSync\"
```

## Linux Deployment

### Installation

```bash
# Setup directories
sudo mkdir -p /opt/brandshare-dam-sync /var/lib/dam-sync /var/log/dam-sync

# Copy binaries
sudo cp publish/linux/worker/linux-x64/* /opt/brandshare-dam-sync/
sudo chmod +x /opt/brandshare-dam-sync/BrandShareDAMSyncd

# Create service user
sudo useradd -r -s /bin/false -d /var/lib/dam-sync dam-sync
sudo chown -R dam-sync:dam-sync /var/lib/dam-sync /var/log/dam-sync
```

### Systemd Service

Create `/etc/systemd/system/brandshare-dam-sync.service`:

```ini
[Unit]
Description=BrandShare DAM Sync Service
After=network.target

[Service]
Type=notify
User=dam-sync
Group=dam-sync
WorkingDirectory=/opt/brandshare-dam-sync
ExecStart=/opt/brandshare-dam-sync/BrandShareDAMSyncd
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### Service Management

```bash
# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable brandshare-dam-sync
sudo systemctl start brandshare-dam-sync

# Check status
sudo systemctl status brandshare-dam-sync

# View logs
sudo journalctl -u brandshare-dam-sync -f
```

## Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
EXPOSE 8080

# Create user
RUN addgroup -g 1001 -S appgroup && \\n    adduser -u 1001 -S appuser -G appgroup

# Copy application
COPY publish/linux/worker/linux-x64/ .
RUN mkdir -p data logs && \\n    chown -R appuser:appgroup /app

USER appuser
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s \\n    CMD wget --spider http://localhost:8080/health || exit 1

ENTRYPOINT [\"./BrandShareDAMSyncd\"]
```

### Docker Compose

```yaml
version: '3.8'
services:
  dam-sync:
    build: .
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - BRANDSHAREDAM__BASEURL=https://your-dam.com
    env_file: .env
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    ports:
      - \"8080:8080\"
```

### Docker Commands

```bash
# Build and start
docker-compose up -d

# View logs
docker-compose logs -f

# Update
docker-compose pull && docker-compose up -d
```

## Configuration Management

### Environment Variables

```bash
# Production settings
export ASPNETCORE_ENVIRONMENT=Production
export BRANDSHAREDAM__BASEURL=https://dam.company.com
export BRANDSHAREDAM__APIKEY=your-api-key
export WORKER__INTERVALSECONDS=300
```

### Configuration Files

```json
{
  \"BrandShareDam\": {
    \"BaseUrl\": \"https://your-dam.com\",
    \"RequestTimeout\": \"00:02:00\"
  },
  \"Worker\": {
    \"IntervalSeconds\": 300,
    \"MaxConcurrentJobs\": 3
  },
  \"Logging\": {
    \"LogLevel\": {
      \"Default\": \"Information\"
    }
  }
}
```

## Monitoring

### Health Checks

```bash
# Check application health
curl -f http://localhost:8080/health

# Check service status
# Windows
sc query \"BrandShare DAM Sync\"

# Linux
systemctl is-active brandshare-dam-sync
```

### Log Locations

- **Windows**: Event Viewer → Application logs
- **Linux**: `/var/log/dam-sync/` or `journalctl`
- **Docker**: `docker logs <container>`

## Maintenance

### Database Backup

```bash
# Stop service
sudo systemctl stop brandshare-dam-sync

# Backup database
sqlite3 /var/lib/dam-sync/dam-sync.db \".backup backup-$(date +%Y%m%d).db\"

# Start service
sudo systemctl start brandshare-dam-sync
```

### Regular Maintenance

```bash
# Database optimization
sqlite3 /var/lib/dam-sync/dam-sync.db \"VACUUM; ANALYZE;\"

# Log rotation
logrotate -f /etc/logrotate.d/brandshare-dam-sync

# Check disk space
df -h /var/lib/dam-sync
```

## Troubleshooting

### Common Issues

1. **Service won't start**
   - Check file permissions
   - Verify configuration
   - Review logs for errors

2. **Database errors**
   - Check SQLite file permissions
   - Ensure adequate disk space
   - Verify database path

3. **Network connectivity**
   - Test DAM API connectivity
   - Check firewall rules
   - Verify API credentials

### Log Analysis

```bash
# Recent errors
grep -E \"ERROR|FATAL\" /var/log/dam-sync/dam-sync.log

# Service restarts
journalctl -u brandshare-dam-sync | grep \"Started\\|Stopped\"

# Performance metrics
grep \"Duration:\" /var/log/dam-sync/dam-sync.log
```

## Security Considerations

- Use dedicated service accounts
- Restrict file system permissions
- Secure API key storage
- Enable log monitoring
- Regular security updates
- Network segmentation

This documentation provides the essential information needed to deploy and operate Brandshare DAM Sync effectively across different environments.