# Transcription System

A scalable, production-ready ASP.NET transcription service using Whisper.cpp with Clean Architecture, supporting both REST API and gRPC protocols.

## 🚀 Features

- **Multiple Protocol Support**: REST API and gRPC
- **Scalable Architecture**: Message queue-based job processing
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Multiple Whisper Models**: Support for Tiny, Base, Small, Medium, and Large models
- **Async Processing**: Background workers for efficient transcription
- **Comprehensive Monitoring**: Health checks, metrics, and detailed logging
- **Docker Support**: Easy deployment with Docker Compose
- **Production Ready**: Systemd services, monitoring, and deployment scripts

## 📋 Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
- [API Documentation](#api-documentation)
- [Deployment](#deployment)
- [Testing](#testing)
- [Contributing](#contributing)

## ⚡ Quick Start

### Using Docker (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd TranscriptionSystem

# Start services
docker-compose up -d

# Check status
docker-compose ps

# Test the API
curl http://localhost:5000/health
```

### Manual Setup on Ubuntu

```bash
# Run setup script
sudo ./scripts/setup-ubuntu.sh

# Download Whisper models
sudo ./scripts/download-models.sh

# Build and run
dotnet build
./scripts/run-services.sh
```

## 🏗️ Architecture

```
TranscriptionSystem/
├── src/
│   ├── TranscriptionService.Domain/        # Core business logic
│   ├── TranscriptionService.Application/   # Use cases and interfaces
│   ├── TranscriptionService.Infrastructure/# External services
│   ├── TranscriptionService.API/           # REST & gRPC endpoints
│   ├── TranscriptionService.Worker/        # Background job processor
│   └── TranscriptionService.Contracts/     # Shared DTOs and Protos
├── tests/                                   # Unit and integration tests
├── scripts/                                 # Deployment and utility scripts
└── docker-compose.yml                       # Container orchestration
```

**Technology Stack:**
- **.NET 8**: Modern, high-performance framework
- **MySQL**: Relational database for job persistence
- **RabbitMQ**: Message queue for job distribution
- **Whisper.cpp**: Fast C++ implementation of OpenAI's Whisper
- **Entity Framework Core**: ORM for database operations
- **MediatR**: CQRS and mediator pattern implementation
- **gRPC**: High-performance RPC framework
- **Docker**: Containerization and deployment

## 📦 Prerequisites

### For Docker Deployment
- Docker 20.10+
- Docker Compose 2.0+
- 4GB RAM minimum (8GB recommended)
- 20GB free disk space

### For Manual Installation
- Ubuntu 20.04 LTS or later
- .NET 8 SDK
- MySQL 8.0+
- RabbitMQ 3.12+
- Build tools (gcc, make, cmake)

## 🛠️ Installation

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed installation instructions.

### Quick Install (Ubuntu)

```bash
# Complete automated setup
sudo ./scripts/setup-ubuntu.sh

# Download Whisper models
sudo ./scripts/download-models.sh
```

## 💻 Usage

### REST API

#### Submit a Transcription Job
```bash
curl -X POST http://localhost:5000/api/transcription/submit \
  -F "AudioFile=@audio.wav" \
  -F "Model=Base" \
  -F "Language=en"
```

#### Check Job Status
```bash
curl http://localhost:5000/api/transcription/{jobId}/status
```

#### Get Transcription Result
```bash
curl http://localhost:5000/api/transcription/{jobId}/result
```

### gRPC

```csharp
// C# example
var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new TranscriptionService.TranscriptionServiceClient(channel);

var request = new SubmitJobRequest
{
    FileName = "audio.wav",
    AudioData = ByteString.CopyFrom(audioBytes),
    Model = "Base"
};

var response = await client.SubmitJobAsync(request);
Console.WriteLine($"Job ID: {response.JobId}");
```

## 📚 API Documentation

### Swagger UI
Access interactive API documentation at: http://localhost:5000

### Available Endpoints

**Transcription:**
- `POST /api/transcription/submit` - Submit new job
- `GET /api/transcription/{id}/status` - Get job status
- `GET /api/transcription/{id}/result` - Get transcription result
- `GET /api/transcription/{id}/result/text` - Get result as plain text
- `DELETE /api/transcription/{id}` - Cancel job

**Health:**
- `GET /health` - Basic health check
- `GET /api/health/detailed` - Detailed component status

### Supported Audio Formats
- WAV (.wav)
- MP3 (.mp3)
- FLAC (.flac)
- M4A (.m4a)
- OGG (.ogg)
- WebM (.webm)

### Whisper Models

| Model  | Size   | Speed      | Accuracy |
|--------|--------|------------|----------|
| Tiny   | 75 MB  | Fastest    | Basic    |
| Base   | 142 MB | Fast       | Good     |
| Small  | 466 MB | Moderate   | Better   |
| Medium | 1.5 GB | Slow       | Great    |
| Large  | 2.9 GB | Slowest    | Best     |

## 🚢 Deployment

### Docker Compose (Production)

```bash
# Configure environment
cp .env.development .env
nano .env

# Start services
docker-compose up -d

# Scale workers
docker-compose up -d --scale worker=5

# View logs
docker-compose logs -f
```

### Systemd Services

```bash
# Install services
sudo ./scripts/install-services.sh

# Start services
sudo systemctl start transcription-api
sudo systemctl start transcription-worker

# Enable auto-start
sudo systemctl enable transcription-api
sudo systemctl enable transcription-worker

# View logs
sudo journalctl -u transcription-api -f
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment guide.

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Projects
```bash
# Unit tests
dotnet test tests/TranscriptionService.Domain.Tests
dotnet test tests/TranscriptionService.Application.Tests

# E2E tests
dotnet test tests/TranscriptionService.E2ETests
```

### Run API Integration Tests
```bash
./scripts/test-api.sh
```

## 📊 Monitoring

### Application Metrics
- Job queue length
- Processing times
- Success/failure rates
- Worker status

### Access Monitoring Tools
- **RabbitMQ Management**: http://localhost:15672
- **API Health Check**: http://localhost:5000/api/health/detailed
- **Logs**: `docker-compose logs -f` or `journalctl`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Whisper.cpp](https://github.com/ggerganov/whisper.cpp) - Fast C++ implementation of Whisper
- [OpenAI Whisper](https://github.com/openai/whisper) - Original Whisper model

## 📧 Support

- **Issues**: [GitHub Issues](<repository-url>/issues)
- **Documentation**: [Wiki](<repository-url>/wiki)
- **Email**: support@transcriptionservice.com

---

Made with ❤️ using Clean Architecture and .NET 8