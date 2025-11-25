# Transcription System

A scalable ASP.NET transcription service using Whisper.cpp with Clean Architecture.

## Architecture

- **Domain Layer**: Core business logic and entities
- **Application Layer**: Use cases and business rules
- **Infrastructure Layer**: External concerns (database, message queue, Whisper.cpp)
- **API Layer**: REST and gRPC endpoints
- **Worker Service**: Background job processing

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL
- RabbitMQ
- Whisper.cpp binary

### Build
```bash
dotnet build
```

### Run
```bash
# API
dotnet run --project src/TranscriptionService.API

# Worker
dotnet run --project src/TranscriptionService.Worker
```

## Testing
```bash
dotnet test
```
