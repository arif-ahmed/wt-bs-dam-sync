# Architecture Documentation

## System Architecture Overview

Brandshare DAM Sync follows **Clean Architecture** principles, ensuring separation of concerns, testability, and maintainability. The system is designed for cross-platform deployment and supports multiple synchronization strategies.

## Architectural Principles

### Clean Architecture Layers

The system is organized into distinct layers with well-defined boundaries:

```mermaid
graph TB
    subgraph "Presentation Layer"
        CLI[CLI Application]
        Daemon[Background Daemon]
    end
    
    subgraph "Application Layer"
        Commands[Commands]
        Queries[Queries]
        Handlers[MediatR Handlers]
        DTOs[Data Transfer Objects]
    end
    
    subgraph "Domain Layer"
        Entities[Domain Entities]
        Enums[Business Enums]
        Rules[Business Rules]
        Interfaces[Domain Interfaces]
    end
    
    subgraph "Infrastructure Layer"
        DAMClient[DAM API Client]
        FileSystem[File System Access]
        Database[SQLite Database]
        S3[S3 Integration]
    end
    
    CLI --> Commands
    Daemon --> Queries
    Commands --> Entities
    Queries --> Entities
    Handlers --> Interfaces
    DAMClient --> Interfaces
    Database --> Interfaces
```

### Dependency Flow

- **Outer layers depend on inner layers**
- **Inner layers are independent of outer layers**
- **Dependencies point inward** (Dependency Inversion Principle)

## Core Components

### 1. Domain Layer

#### Entities

- **SyncJob**: Represents a synchronization job configuration
  - Properties: JobName, VolumePath, SyncDirection, JobInterval
  - Navigation: Related Tenant, tracking metadata

- **FileEntity**: Tracks individual file metadata and sync state
  - Properties: FileName, FilePath, ModifiedAt, ChecksumHash
  - Audit: CreatedAt, LastModified, LastSeenSyncId

- **Folder**: Directory structure tracking
  - Properties: Path, Label, ParentId, IsActive
  - Hierarchy: Parent-child relationships

- **Tenant**: Multi-tenant configuration
  - Properties: BaseUrl, ApiKey, Domain
  - Isolation: Tenant-scoped data access

#### Enums

- **SyncJobStatus**: Created, Running, Succeeded, Failed, Cancelled
- **TrackedItemState**: Pending, Queued, Processing, Synced, Deleted, Failed
- **SyncDirection**: L2D (Local to DAM), D2L (DAM to Local), Both
- **FileChangeKind**: Created, Changed, Deleted, Renamed

### 2. Application Layer

#### CQRS Implementation

**Commands** (Write Operations):
- `CreateJobCommand`: Creates new sync jobs
- Command handlers implement business logic
- Use `IUnitOfWork` for transactional consistency

**Queries** (Read Operations):
- `GetJobByIdQuery`: Retrieves specific job details
- `GetTenantsQuery`: Lists configured tenants
- Query handlers optimize for read scenarios

#### MediatR Integration

```csharp
// Example command handling
public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<JobDto> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        // Business logic implementation
        // Repository operations through UnitOfWork
        // Return result
    }
}
```

### 3. Infrastructure Layer

#### DAM API Client

Built with **Refit** for type-safe HTTP client generation:

```csharp
public interface IBrandShareDamApi
{
    [Get("/FileSync/GetFolders")]
    Task<ApiResponse<List<FolderSummary>>> GetFolders(
        [Header("TenantId")] string tenantId, 
        [AliasAs("jobId")] string jobId);
        
    [Post("/FileSync/CreateItemV2")]
    Task<ApiResponse<CreateItemResponse>> CreateItemV2(
        [Header("TenantId")] string tenantId, 
        [Body] CreateItemV2Request request);
}
```

**Features**:
- Automatic JSON serialization/deserialization
- Header-based authentication
- Tenant-aware requests
- Comprehensive endpoint coverage

#### Resilience Patterns (Polly)

```csharp
services.AddRefitClient<IBrandShareDamApi>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: attempt => 
            TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

#### Persistence Layer

**Entity Framework Core** with **SQLite**:

- **DbContext**: `DamSyncDbContext` with audit interceptors
- **Repositories**: Generic repository pattern implementation
- **Unit of Work**: Transactional consistency across operations
- **Migrations**: Database schema versioning

**Key Features**:
- Soft delete with global query filters
- Automatic audit trail (Created/Modified timestamps)
- Tenant isolation at data level

### 4. Sync Strategy Pattern

#### Job Executor Architecture

```mermaid
graph LR
    JobPoller[Job Poller] --> Factory[Executor Factory]
    Factory --> |JobType.Download| Download[OneWayDownloadJobExecutor]
    Factory --> |JobType.Upload| Upload[OneWayUploadJobExecutor]
    Factory --> |JobType.BiDirectional| BiDir[BiDirectionalSyncJobExecutor]
    Factory --> |JobType.UploadAndCleanup| UploadClean[UploadAndCleanJobExecutor]
    Factory --> |JobType.DownloadAndCleanup| DownloadClean[DownloadAndCleanJobExecutor]
```

#### Strategy Implementation

```csharp
public interface IJobExecutorService
{
    Task ExecuteJobAsync((string syncId, string tenantId, string jobId) syncJobInfo, 
                        CancellationToken ct = default);
}

// Registered with keyed services
services.AddKeyedTransient<IJobExecutorService, OneWayDownloadJobExecutor>(SyncJobType.DOWNLOAD);
services.AddKeyedTransient<IJobExecutorService, OneWayUploadJobExecutor>(SyncJobType.UPLOAD);
```

## Sync Flow Architecture

### Download Sync Flow

```mermaid
sequenceDiagram
    participant D as Daemon
    participant API as DAM API
    participant DB as Database
    participant FS as File System
    
    D->>API: GetModifiedItems()
    API-->>D: Modified items list
    
    loop For each modified item
        D->>DB: Check local tracking
        
        alt Item exists locally
            D->>D: Determine sync action
            alt Path/name changed
                D->>FS: Move/rename file
            else Content changed
                D->>API: Download file
                D->>FS: Update local file
            end
        else New item
            D->>API: Download file
            D->>FS: Create local file
        end
        
        D->>DB: Update tracking metadata
    end
```

### Upload Sync Flow

```mermaid
sequenceDiagram
    participant D as Daemon
    participant FS as File System
    participant API as DAM API
    participant S3 as S3 Storage
    participant DB as Database
    
    D->>FS: Scan local directory
    FS-->>D: File list with metadata
    
    loop For each file
        D->>DB: Check tracking state
        
        alt File modified/new
            D->>API: GetUploadDetails()
            API-->>D: S3 upload URL + metadata
            
            D->>S3: Upload file to S3
            S3-->>D: Upload confirmation
            
            D->>API: CreateItemV2()
            API-->>D: DAM item created
            
            D->>DB: Update tracking metadata
        end
    end
```

## Configuration Architecture

### Multi-Tenant Support

```csharp
public class TenantContext : ITenantContext
{
    public async Task<TenantConfig> GetTenantConfigAsync(string tenantId)
    {
        // Retrieve tenant-specific configuration
        // BaseUrl, ApiKey, additional settings
    }
}

public class TenantConfigHandler : DelegatingHandler
{
    // Dynamically sets BaseAddress and Authorization headers
    // Based on current tenant context
}
```

### Dependency Injection

```csharp
// Application layer registration
services.AddMediatR(Assembly.GetExecutingAssembly());

// Infrastructure layer registration
services.AddRefitClient<IBrandShareDamApi>()
    .ConfigureHttpClient(/* tenant-aware configuration */)
    .AddHttpMessageHandler<TenantConfigHandler>();

// Persistence layer registration
services.AddDbContext<DamSyncDbContext>(options =>
    options.UseSqlite(connectionString));
```

## Background Processing

### Worker Service Architecture

```mermaid
graph TB
    subgraph "Background Services"
        W[Worker Service]
        MP[MachinePoller]
        JP[JobPoller]
    end
    
    subgraph "Job Processing"
        JF[JobExecutorFactory]
        JE1[OneWayDownload]
        JE2[OneWayUpload]
        JE3[BiDirectionalSync]
    end
    
    subgraph "Coordination"
        DC[DownloadCoordinator]
        TL[ThrottlingLimiter]
    end
    
    W --> MP
    W --> JP
    JP --> JF
    JF --> JE1
    JF --> JE2
    JF --> JE3
    JE1 --> DC
    JE2 --> TL
```

### Hosted Service Implementation

```csharp
public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(5);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSyncJobs(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync cycle failed");
            }
            
            await Task.Delay(interval, stoppingToken);
        }
    }
}
```

## Security Architecture

### API Authentication

- **Header-based authentication** with tenant-specific API keys
- **Dynamic header injection** via HTTP message handlers
- **Secure key storage** in configuration or key vaults

### File System Security

- **Restricted directory access** based on job configurations
- **Path validation** to prevent directory traversal
- **Permission checking** before file operations

### Tenant Isolation

- **Database-level isolation** with tenant ID filtering
- **API request isolation** with tenant headers
- **Configuration isolation** per tenant instance

## Scalability Considerations

### Concurrent Processing

- **Download coordination** with configurable limits
- **Async/await patterns** throughout
- **Cancellation token propagation** for graceful shutdown

### Resource Management

- **Memory-efficient streaming** for large files
- **Connection pooling** for HTTP clients
- **Database connection management** via EF Core

### Monitoring and Observability

- **Structured logging** with correlation IDs
- **Performance counters** for sync operations
- **Health checks** for external dependencies

## Deployment Architecture

### Cross-Platform Support

- **.NET 9 runtime** for cross-platform compatibility
- **Self-contained deployments** with runtime bundling
- **Platform-specific optimizations** via publish profiles

### Service Deployment

- **Windows Service** support with service registration
- **Systemd integration** for Linux environments
- **Docker containerization** capabilities

### Configuration Management

- **Environment-specific settings** via appsettings.json
- **User secrets** for development environments
- **Environment variables** for production configuration

This architecture ensures maintainability, scalability, and testability while providing robust synchronization capabilities between local file systems and DAM instances.