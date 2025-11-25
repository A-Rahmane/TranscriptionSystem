# Whisper Transcription Service - Implementation Roadmap

## Project Overview
A scalable ASP.NET transcription service using Whisper.cpp, implementing Clean Architecture with both HTTP REST API and gRPC support, deployed on Ubuntu.

---

## Solution Structure
```
TranscriptionSystem/
├── src/
│   ├── TranscriptionService.Domain/
│   ├── TranscriptionService.Application/
│   ├── TranscriptionService.Infrastructure/
│   ├── TranscriptionService.API/
│   ├── TranscriptionService.Worker/
│   └── TranscriptionService.Contracts/
├── tests/
│   ├── TranscriptionService.Domain.Tests/
│   ├── TranscriptionService.Application.Tests/
│   └── TranscriptionService.Infrastructure.Tests/
└── docs/
```

---

## Phase 1: Solution & Project Setup

### 1.1 Create Solution
```bash
dotnet new sln -n TranscriptionSystem
```

### 1.2 Create Projects
```bash
# Domain Layer (Class Library)
dotnet new classlib -n TranscriptionService.Domain -o src/TranscriptionService.Domain

# Application Layer (Class Library)
dotnet new classlib -n TranscriptionService.Application -o src/TranscriptionService.Application

# Infrastructure Layer (Class Library)
dotnet new classlib -n TranscriptionService.Infrastructure -o src/TranscriptionService.Infrastructure

# API Layer (Web API)
dotnet new webapi -n TranscriptionService.API -o src/TranscriptionService.API

# Worker Service
dotnet new worker -n TranscriptionService.Worker -o src/TranscriptionService.Worker

# Contracts (Shared DTOs/Protos)
dotnet new classlib -n TranscriptionService.Contracts -o src/TranscriptionService.Contracts

# Add all projects to solution
dotnet sln add src/**/*.csproj
```

---

## Phase 2: Domain Layer (Core Business Logic)

### File: `src/TranscriptionService.Domain/Entities/TranscriptionJob.cs`
**Purpose**: Core entity representing a transcription job
**Content**:
- JobId (Guid)
- FileName, FilePath, FileSize
- Status (Enum: Queued, Processing, Completed, Failed)
- Language, Model
- CreatedAt, StartedAt, CompletedAt
- RetryCount, MaxRetries
- Result text
- Error messages

### File: `src/TranscriptionService.Domain/Entities/TranscriptionResult.cs`
**Purpose**: Value object containing transcription output
**Content**:
- Text (full transcription)
- Segments (timestamped chunks)
- Language detected
- Confidence scores
- Processing duration

### File: `src/TranscriptionService.Domain/Enums/JobStatus.cs`
**Purpose**: Define all possible job states
**Content**:
```csharp
Queued, Processing, Completed, Failed, Cancelled, Retrying
```

### File: `src/TranscriptionService.Domain/Enums/WhisperModel.cs`
**Purpose**: Available Whisper model types
**Content**:
```csharp
Tiny, Base, Small, Medium, Large
```

### File: `src/TranscriptionService.Domain/Exceptions/DomainException.cs`
**Purpose**: Base exception for domain-specific errors
**Content**: Custom exception hierarchy

### File: `src/TranscriptionService.Domain/Interfaces/ITranscriptionJobRepository.cs`
**Purpose**: Repository contract for persistence
**Content**:
- AddAsync(TranscriptionJob)
- GetByIdAsync(Guid)
- UpdateAsync(TranscriptionJob)
- GetQueuedJobsAsync()
- GetJobsByStatusAsync(JobStatus)

### File: `src/TranscriptionService.Domain/ValueObjects/AudioFile.cs`
**Purpose**: Encapsulate audio file information
**Content**:
- FilePath, Format, Duration, SampleRate
- Validation logic

---

## Phase 3: Application Layer (Use Cases)

### File: `src/TranscriptionService.Application/UseCases/SubmitTranscriptionJob/SubmitTranscriptionJobCommand.cs`
**Purpose**: Command to submit new transcription job
**Content**: IFormFile, Language, Model preferences

### File: `src/TranscriptionService.Application/UseCases/SubmitTranscriptionJob/SubmitTranscriptionJobHandler.cs`
**Purpose**: Handler for job submission
**Content**:
- Validate audio file
- Save file to storage
- Create TranscriptionJob entity
- Enqueue to message broker
- Return JobId

### File: `src/TranscriptionService.Application/UseCases/GetJobStatus/GetJobStatusQuery.cs`
**Purpose**: Query job status by ID
**Content**: JobId input

### File: `src/TranscriptionService.Application/UseCases/GetJobStatus/GetJobStatusHandler.cs`
**Purpose**: Retrieve job status and results
**Content**: Query repository, map to DTO

### File: `src/TranscriptionService.Application/UseCases/ProcessTranscriptionJob/ProcessTranscriptionJobCommand.cs`
**Purpose**: Command for worker to process job
**Content**: JobId to process

### File: `src/TranscriptionService.Application/UseCases/ProcessTranscriptionJob/ProcessTranscriptionJobHandler.cs`
**Purpose**: Core transcription logic
**Content**:
- Update job status to Processing
- Call Whisper.cpp wrapper
- Parse results
- Update job with results or errors
- Handle retries on failure

### File: `src/TranscriptionService.Application/Interfaces/IWhisperService.cs`
**Purpose**: Interface for Whisper.cpp interaction
**Content**:
- TranscribeAsync(filePath, model, language)
- Returns TranscriptionResult

### File: `src/TranscriptionService.Application/Interfaces/IMessageQueue.cs`
**Purpose**: Abstract message queue operations
**Content**:
- EnqueueJobAsync(jobId)
- DequeueJobAsync()
- AcknowledgeAsync(messageId)

### File: `src/TranscriptionService.Application/Interfaces/IFileStorage.cs`
**Purpose**: Abstract file storage operations
**Content**:
- SaveAudioFileAsync(stream, fileName)
- GetAudioFilePathAsync(jobId)
- DeleteAudioFileAsync(jobId)

### File: `src/TranscriptionService.Application/DTOs/JobStatusDto.cs`
**Purpose**: Data transfer object for job status
**Content**: Public-facing job information

### File: `src/TranscriptionService.Application/DTOs/SubmitJobResponseDto.cs`
**Purpose**: Response after job submission
**Content**: JobId, estimated wait time

### File: `src/TranscriptionService.Application/Common/Behaviors/ValidationBehavior.cs`
**Purpose**: MediatR pipeline behavior for validation
**Content**: Validate commands using FluentValidation

### File: `src/TranscriptionService.Application/Common/Behaviors/LoggingBehavior.cs`
**Purpose**: MediatR pipeline for logging
**Content**: Log request/response

### File: `src/TranscriptionService.Application/DependencyInjection.cs`
**Purpose**: Register Application layer services
**Content**: MediatR, FluentValidation, AutoMapper

---

## Phase 4: Infrastructure Layer (External Concerns)

### File: `src/TranscriptionService.Infrastructure/Persistence/ApplicationDbContext.cs`
**Purpose**: EF Core DbContext
**Content**: DbSet<TranscriptionJob>, configuration

### File: `src/TranscriptionService.Infrastructure/Persistence/Configurations/TranscriptionJobConfiguration.cs`
**Purpose**: EF Core entity configuration
**Content**: Table mapping, indexes, relationships

### File: `src/TranscriptionService.Infrastructure/Persistence/Repositories/TranscriptionJobRepository.cs`
**Purpose**: Concrete implementation of ITranscriptionJobRepository
**Content**: EF Core queries, CRUD operations

### File: `src/TranscriptionService.Infrastructure/Services/WhisperCppService.cs`
**Purpose**: Wrapper for Whisper.cpp executable
**Content**:
- Execute whisper.cpp via Process
- Parse output (JSON or text)
- Handle errors and timeouts
- Model path management

### File: `src/TranscriptionService.Infrastructure/Services/RabbitMqMessageQueue.cs`
**Purpose**: RabbitMQ implementation of IMessageQueue
**Content**:
- Connection management
- Queue declaration
- Publish/consume messages
- Dead letter queue handling

### File: `src/TranscrationService.Infrastructure/Services/LocalFileStorage.cs`
**Purpose**: Local filesystem storage implementation
**Content**:
- Save uploaded files
- Organize by date/jobId
- Cleanup old files

### File: `src/TranscriptionService.Infrastructure/Services/BackgroundJobService.cs`
**Purpose**: Background service for dequeuing jobs
**Content**:
- Continuous polling
- Call ProcessTranscriptionJobHandler
- Error handling and retries

### File: `src/TranscriptionService.Infrastructure/Configuration/WhisperOptions.cs`
**Purpose**: Configuration for Whisper.cpp
**Content**:
- ExecutablePath
- ModelsPath
- DefaultModel
- MaxConcurrentJobs
- Timeout settings

### File: `src/TranscriptionService.Infrastructure/Configuration/RabbitMqOptions.cs`
**Purpose**: RabbitMQ connection settings
**Content**: Host, Port, Username, Password, QueueName

### File: `src/TranscriptionService.Infrastructure/DependencyInjection.cs`
**Purpose**: Register Infrastructure services
**Content**: DbContext, repositories, external services

### File: `src/TranscriptionService.Infrastructure/Migrations/`
**Purpose**: EF Core migrations
**Content**: Database schema versions

---

## Phase 5: Contracts Layer (Shared)

### File: `src/TranscriptionService.Contracts/Protos/transcription.proto`
**Purpose**: gRPC service definition
**Content**:
```protobuf
service TranscriptionService {
  rpc SubmitJob(SubmitJobRequest) returns (SubmitJobResponse);
  rpc GetJobStatus(GetJobStatusRequest) returns (GetJobStatusResponse);
  rpc StreamJobUpdates(StreamRequest) returns (stream JobUpdate);
}
```

### File: `src/TranscriptionService.Contracts/Http/Requests/SubmitJobRequest.cs`
**Purpose**: HTTP API request model
**Content**: Validation attributes, Swagger documentation

### File: `src/TranscriptionService.Contracts/Http/Responses/JobStatusResponse.cs`
**Purpose**: HTTP API response model
**Content**: Serialization attributes

---

## Phase 6: API Layer (Presentation)

### File: `src/TranscriptionService.API/Controllers/TranscriptionController.cs`
**Purpose**: REST API endpoints
**Content**:
- POST /api/transcription/submit (multipart/form-data)
- GET /api/transcription/{jobId}/status
- GET /api/transcription/{jobId}/result
- DELETE /api/transcription/{jobId}

### File: `src/TranscriptionService.API/GrpcServices/TranscriptionGrpcService.cs`
**Purpose**: gRPC service implementation
**Content**: Implement proto service methods

### File: `src/TranscriptionService.API/Middleware/ExceptionHandlingMiddleware.cs`
**Purpose**: Global exception handler
**Content**: Catch exceptions, return proper HTTP status codes

### File: `src/TranscriptionService.API/Program.cs`
**Purpose**: Application entry point
**Content**:
- Configure services
- Add middleware pipeline
- Configure gRPC
- Add Swagger/OpenAPI

### File: `src/TranscriptionService.API/appsettings.json`
**Purpose**: Configuration settings
**Content**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Whisper": {
    "ExecutablePath": "/usr/local/bin/whisper",
    "ModelsPath": "/var/models/whisper"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672
  },
  "FileStorage": {
    "BasePath": "/var/transcription-files"
  }
}
```

### File: `src/TranscriptionService.API/Dockerfile`
**Purpose**: Container definition for API
**Content**: Multi-stage build, runtime dependencies

---

## Phase 7: Worker Service

### File: `src/TranscriptionService.Worker/Worker.cs`
**Purpose**: Background service host
**Content**:
- Inherit BackgroundService
- Call BackgroundJobService from Infrastructure
- Graceful shutdown handling

### File: `src/TranscriptionService.Worker/Program.cs`
**Purpose**: Worker entry point
**Content**:
- Configure DI
- Register Infrastructure services
- Setup logging

### File: `src/TranscriptionService.Worker/appsettings.json`
**Purpose**: Worker-specific configuration
**Content**: Same structure as API settings

### File: `src/TranscriptionService.Worker/Dockerfile`
**Purpose**: Container definition for worker
**Content**: Include whisper.cpp binary and models

---

## Phase 8: Deployment & Infrastructure

### File: `docker-compose.yml`
**Purpose**: Orchestrate all services locally
**Content**:
- API service
- Worker service(s)
- PostgreSQL database
- RabbitMQ
- Volume mounts for models and files

### File: `scripts/install-whisper-cpp.sh`
**Purpose**: Setup Whisper.cpp on Ubuntu
**Content**:
```bash
# Clone repo
# Build with make
# Download models
# Set permissions
```

### File: `scripts/download-models.sh`
**Purpose**: Download Whisper models
**Content**: wget/curl commands for model files

### File: `.github/workflows/ci-cd.yml` (optional)
**Purpose**: CI/CD pipeline
**Content**: Build, test, deploy steps

### File: `nginx.conf`
**Purpose**: Reverse proxy configuration
**Content**: Route traffic to API, load balancing for workers

---

## Phase 9: Testing

### Structure:
- Unit tests for Domain entities
- Integration tests for UseCases (Application layer)
- Integration tests for Infrastructure (db, queue)
- End-to-end tests for API endpoints

---

## Implementation Order

1. **Setup** (Day 1)
   - Create solution and projects
   - Add project references
   - Install NuGet packages

2. **Domain Layer** (Day 1)
   - Entities, enums, interfaces
   - No external dependencies

3. **Application Layer** (Day 2-3)
   - Use cases and handlers
   - DTOs and interfaces
   - MediatR setup

4. **Infrastructure - Persistence** (Day 3-4)
   - DbContext and repositories
   - Migrations
   - Database setup

5. **Infrastructure - Whisper Integration** (Day 4-5)
   - WhisperCppService implementation
   - Test with actual whisper.cpp binary
   - Error handling

6. **Infrastructure - Message Queue** (Day 5)
   - RabbitMQ setup
   - Queue service implementation

7. **API Layer** (Day 6)
   - REST Controllers
   - Middleware
   - Swagger setup

8. **gRPC Integration** (Day 7)
   - Proto definitions
   - gRPC service implementation
   - Client testing

9. **Worker Service** (Day 7-8)
   - Background service implementation
   - Job processing loop
   - Error recovery

10. **Deployment** (Day 9-10)
    - Docker images
    - docker-compose setup
    - Ubuntu deployment scripts
    - Testing end-to-end

---

## Key NuGet Packages

**All Projects:**
- Microsoft.Extensions.Logging

**Domain:**
- (No external dependencies)

**Application:**
- MediatR
- FluentValidation
- AutoMapper

**Infrastructure:**
- Microsoft.EntityFrameworkCore.PostgreSQL
- RabbitMQ.Client
- Dapper (optional, for performance)

**API:**
- Swashbuckle.AspNetCore
- Grpc.AspNetCore
- Microsoft.AspNetCore.Authentication.JwtBearer (if auth needed)

**Contracts:**
- Grpc.Tools
- Google.Protobuf

---

## Ubuntu Deployment Prerequisites

1. .NET 8 SDK/Runtime
2. PostgreSQL
3. RabbitMQ
4. Whisper.cpp compiled binary
5. Whisper model files (ggml-*.bin)
6. Nginx (for reverse proxy)
7. systemd service files for API and Worker

---

## Configuration Management

- Use appsettings.json for defaults
- Use appsettings.Production.json for overrides
- Use environment variables in production
- Use User Secrets for local development

---

## Monitoring & Observability

- Structured logging (Serilog)
- Health checks endpoint
- Metrics (Prometheus-compatible)
- Distributed tracing (OpenTelemetry) - optional

---

## Security Considerations

- Authentication/Authorization on API endpoints
- File upload validation (size, format)
- Rate limiting
- Input sanitization
- Secure file storage permissions
