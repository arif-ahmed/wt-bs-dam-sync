# Brandshare DAM Sync

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey.svg)]()

A robust, cross-platform synchronization tool for Brandshare Digital Asset Management (DAM) systems, enabling reliable, configurable, and efficient file transfers between local filesystems and DAM repositories.

## 🚀 Features

- **Bi-directional Synchronization** - Upload, download, or sync in both directions
- **Multiple Sync Strategies** - OneWayUpload, OneWayDownload, BiDirectionalSync, UploadAndClean, DownloadAndClean
- **Background Daemon** - Runs as a service with scheduled job execution
- **Multi-tenant Support** - Manage multiple DAM instances and API configurations
- **Delta Sync** - Only transfers changed files for optimal performance
- **Resilient Operations** - Built-in retry policies, circuit breakers, and error handling
- **Cross-platform** - Supports Windows, Linux, and macOS
- **CLI Management** - Interactive and command-line interfaces for configuration

## 📋 Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Sync Strategies](#sync-strategies)
- [Development](#development)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## ⚡ Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git
- Access to a Brandshare DAM instance with API credentials

### Build and Run

```bash
# Clone the repository
git clone <repository-url>
cd wt-bs-dam-sync

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the CLI (interactive mode)
dotnet run --project src/BrandShareDAMSync.Cli

# Run the daemon service
dotnet run --project src/BrandShareDAMSync.Daemon
```

## 🏗️ Architecture

Brandshare DAM Sync follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                   │
│  ┌─────────────────────┐  ┌─────────────────────────┐   │
│  │        CLI          │  │       Daemon            │   │
│  │   (User Interface)  │  │  (Background Service)   │   │
│  └─────────────────────┘  └─────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                      │
│     Commands, Queries, DTOs, Business Logic (MediatR)  │
└─────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────┐
│                    Domain Layer                         │
│    Entities, Enums, Business Rules, Domain Services    │
└─────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────┐
│                Infrastructure Layer                     │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐ │
│  │ DAM API     │ │ File System │ │    Persistence      │ │
│  │ (Refit)     │ │   (Local)   │ │ (EF Core + SQLite) │ │
│  └─────────────┘ └─────────────┘ └─────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### Key Components

- **Domain Models**: `SyncJob`, `FileEntity`, `Folder`, `Tenant`
- **Sync Strategies**: Pluggable strategies for different sync behaviors
- **Job Executors**: Process sync jobs with specific strategies
- **DAM API Client**: Refit-based client with Polly resilience
- **Persistence**: SQLite database with EF Core for metadata tracking

## 📦 Installation

### From Source

```bash
git clone <repository-url>
cd wt-bs-dam-sync
dotnet restore
dotnet build
```

### Pre-built Binaries

Use the publish scripts to create platform-specific binaries:

```bash
# Windows
.\scripts\publish.ps1

# Linux/macOS
./scripts/publish.sh
```

This creates self-contained executables in the `publish/` directory for multiple platforms.

## ⚙️ Configuration

### Tenant Configuration

Configure DAM instances using the CLI:

```bash
# Interactive mode
dotnet run --project src/BrandShareDAMSync.Cli

# Command-line mode
dotnet run --project src/BrandShareDAMSync.Cli -- tenants add \
  --base-url "https://your-dam.com" \
  --api-key "your-api-key" \
  --domain "your-domain"
```

### Application Settings

Edit `appsettings.json` in the Daemon project:

```json
{
  "Worker": {
    "IntervalSeconds": 300
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## 📖 Usage

### CLI Commands

```bash
# List all tenants
dotnet run --project src/BrandShareDAMSync.Cli -- tenants list

# Add a new tenant
dotnet run --project src/BrandShareDAMSync.Cli -- tenants add \
  --base-url "https://dam.company.com" \
  --api-key "your-api-key"

# Update tenant
dotnet run --project src/BrandShareDAMSync.Cli -- tenants update \
  --id "tenant-id" \
  --base-url "https://new-url.com"

# Delete tenant
dotnet run --project src/BrandShareDAMSync.Cli -- tenants delete --id "tenant-id"
```

### Running as a Service

#### Windows Service

```powershell
# Install as Windows Service
sc create "BrandShare DAM Sync" binPath="C:\path\to\BrandShareDAMSyncd.exe"
sc start "BrandShare DAM Sync"
```

#### Linux Systemd

```bash
# Create service file
sudo nano /etc/systemd/system/brandshare-dam-sync.service

# Enable and start
sudo systemctl enable brandshare-dam-sync
sudo systemctl start brandshare-dam-sync
```

## 🔄 Sync Strategies

### OneWayUpload
Synchronizes files from local filesystem to DAM only.

```csharp
// Automatically selected based on job configuration
JobType.Upload → OneWayUploadJobExecutor
```

### OneWayDownload
Synchronizes files from DAM to local filesystem only.

```csharp
JobType.Download → OneWayDownloadJobExecutor
```

### BiDirectionalSync
Synchronizes files in both directions with conflict resolution.

```csharp
JobType.BiDirectional → BiDirectionalSyncJobExecutor
```

### UploadAndClean / DownloadAndClean
Sync files and clean up source location after successful transfer.

## 🛠️ Development

### Project Structure

```
src/
├── BrandShareDAMSync.Abstractions/     # Interfaces and contracts
├── BrandShareDAMSync.Application/      # Business logic (CQRS)
├── BrandShareDAMSync.Cli/             # Command-line interface
├── BrandShareDAMSync.Daemon/          # Background service
├── BrandShareDAMSync.Domain/          # Domain models and business rules
├── BrandShareDAMSync.Infrastructure/   # External integrations
└── BrandShareDAMSync.Infrastructure.Persistence/ # Data access
```

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Run tests
dotnet test
```

### Database Migrations

```bash
# Windows
.\scripts\migrate.ps1

# Linux/macOS
./scripts/migrate.sh
```

## 🚀 Deployment

### Self-Contained Deployment

The publish scripts create self-contained deployments that don't require .NET runtime on target machines:

```bash
# Creates binaries for Windows, Linux, and macOS
.\scripts\publish.ps1  # Windows
./scripts/publish.sh   # Linux/macOS
```

### Docker (Optional)

```dockerfile
# Example Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/linux/worker/linux-x64/ .
ENTRYPOINT ["./BrandShareDAMSyncd"]
```

## 🔧 Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Check SQLite file permissions
   - Verify database path in configuration

2. **API Authentication Failures**
   - Validate API key and base URL
   - Check network connectivity to DAM instance

3. **File Permission Errors**
   - Ensure service account has read/write access to sync directories
   - Check file locking by other processes

### Logging

Logs are written to:
- **Development**: Console and local log files
- **Production**: System event logs and structured log files

```bash
# View daemon logs (Linux)
tail -f /var/log/dam-sync/dam-sync.log

# View daemon logs (Windows)
# Check Windows Event Viewer → Applications and Services → BrandshareDamSync
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the existing code style
4. Add tests for new functionality
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Code Style

- Follow Clean Architecture principles
- Use dependency injection for all external dependencies
- Add XML documentation for public APIs
- Include unit tests for business logic
- Follow C# naming conventions

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support and questions:

- Create an issue in the repository
- Check the [Troubleshooting](#troubleshooting) section
- Review the [Architecture Documentation](docs/architecture.md)

---

**Built with ❤️ using .NET 9 and Clean Architecture principles**
