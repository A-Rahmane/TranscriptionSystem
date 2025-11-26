# Transcription Service - Deployment Guide

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Quick Start with Docker](#quick-start-with-docker)
3. [Manual Installation on Ubuntu](#manual-installation-on-ubuntu)
4. [Configuration](#configuration)
5. [Running the Services](#running-the-services)
6. [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)
7. [Production Considerations](#production-considerations)

---

## Prerequisites

### System Requirements
- Ubuntu 20.04 LTS or later
- Minimum 4GB RAM (8GB recommended)
- 20GB free disk space (more for audio files and models)
- CPU with at least 2 cores (4+ recommended)

### Software Requirements
- .NET 8 SDK
- MySQL 8.0+
- RabbitMQ 3.12+
- Whisper.cpp

---

## Quick Start with Docker

### 1. Clone the Repository
```bash
git clone <repository-url>
cd TranscriptionSystem
```

### 2. Configure Environment
```bash
cp .env.development .env
# Edit .env with your settings
nano .env
```

### 3. Download Whisper Models
```bash
# Create models directory
mkdir -p whisper-models

# Download models (choose based on your needs)
cd whisper-models
wget https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
wget https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
cd ..
```

### 4. Start Services
```bash
docker-compose up -d
```

### 5. Check Status
```bash
docker-compose ps
docker-compose logs -f
```

### 6. Test the API
```bash
# Health check
curl http://localhost:5000/health

# Submit a job
curl -F "AudioFile=@test-audio.wav" \
     -F "Model=Base" \
     http://localhost:5000/api/transcription/submit
```

### 7. Access Services
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

---

## Manual Installation on Ubuntu

### 1. Run Setup Script
```bash
sudo ./scripts/setup-ubuntu.sh
```

This will install:
- .NET 8 SDK
- MySQL
- RabbitMQ
- Whisper.cpp
- All dependencies

### 2. Download Whisper Models
```bash
sudo ./scripts/download-models.sh
```

Choose the models you want (recommended: Base + Small).

### 3. Configure Database
```bash
# MySQL should be installed and running
sudo systemctl status mysql

# The setup script creates the database, but you can verify:
mysql -u transcription_user -p transcription_db
# Password: transcription_pass_2024
```

### 4. Update Configuration

Edit `src/TranscriptionService.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=transcription_db;User=transcription_user;Password=transcription_pass_2024;"
  }
}
```

### 5. Build the Solution
```bash
dotnet build
```

### 6. Run Database Migrations
```bash
cd src/TranscriptionService.API
dotnet ef database update
cd ../..
```

### 7. Run Services

**Option A: Development Mode**
```bash
./scripts/run-services.sh
```

**Option B: Production Mode with systemd**
```bash
# Publish the applications
dotnet publish src/TranscriptionService.API -c Release -o /opt/transcription-service
dotnet publish src/TranscriptionService.Worker -c Release -o /opt/transcription-service

# Install systemd services
sudo ./scripts/install-services.sh

# Start services
sudo systemctl start transcription-api
sudo systemctl start transcription-worker

# Check status
sudo systemctl status transcription-api
sudo systemctl status transcription-worker
```

---

## Configuration

### Database Configuration

**MySQL Connection String Format:**
```
Server=<host>;Database=<database>;User=<username>;Password=<password>;
```

### RabbitMQ Configuration

**appsettings.json:**
```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "QueueName": "transcription-jobs"
  }
}
```

### Whisper Configuration

**appsettings.json:**
```json
{
  "Whisper": {
    "ExecutablePath": "/usr/local/bin/whisper",
    "ModelsPath": "/var/models/whisper",
    "MaxConcurrentJobs": 2,
    "Threads": 4,
    "EnableGpu": false
  }
}
```

### File Storage Configuration

**appsettings.json:**
```json
{
  "FileStorage": {
    "BasePath": "/var/transcription-files",
    "RetentionDays": 7,
    "EnableAutoCleanup": true
  }
}
```

---

## Running the Services

### Development Mode

**Terminal 1 - API:**
```bash
cd src/TranscriptionService.API
dotnet run
```

**Terminal 2 - Worker:**
```bash
cd src/TranscriptionService.Worker
dotnet run
```

### Production Mode

```bash
# Using systemd
sudo systemctl start transcription-api
sudo systemctl start transcription-worker

# Enable auto-start on boot
sudo systemctl enable transcription-api
sudo systemctl enable transcription-worker
```

### Docker Mode

```bash
# Start all services
docker-compose up -d

# Scale workers
docker-compose up -d --scale worker=3

# Stop services
docker-compose down

# View logs
docker-compose logs -f api
docker-compose logs -f worker
```

---

## Monitoring and Troubleshooting

### View Logs

**systemd services:**
```bash
# API logs
sudo journalctl -u transcription-api -f

# Worker logs
sudo journalctl -u transcription-worker -f
```

**Docker:**
```bash
docker-compose logs -f
```

### Check Service Status

**API Health Check:**
```bash
curl http://localhost:5000/health
curl http://localhost:5000/api/health/detailed
```

**Database:**
```bash
mysql -u transcription_user -p -e "USE transcription_db; SELECT COUNT(*) FROM TranscriptionJobs;"
```

**RabbitMQ:**
```bash
# Management UI
http://localhost:15672

# CLI
sudo rabbitmqctl list_queues
```

### Common Issues

**Issue: Database connection fails**
```bash
# Check MySQL is running
sudo systemctl status mysql

# Test connection
mysql -u transcription_user -p transcription_db

# Check connection string in appsettings.json
```

**Issue: Worker not processing jobs**
```bash
# Check RabbitMQ
sudo systemctl status rabbitmq-server
sudo rabbitmqctl list_queues

# Check worker logs
sudo journalctl -u transcription-worker -n 100

# Restart worker
sudo systemctl restart transcription-worker
```

**Issue: Whisper transcription fails**
```bash
# Verify Whisper installation
/usr/local/bin/whisper --help

# Check models exist
ls -lh /var/models/whisper/

# Test manually
/usr/local/bin/whisper -m /var/models/whisper/ggml-base.bin -f test-audio.wav
```

---

## Production Considerations

### Security

1. **Change Default Passwords**
   - MySQL
   - RabbitMQ
   - Any other services

2. **Enable HTTPS**
   - Use reverse proxy (Nginx/Apache)
   - Configure SSL certificates

3. **Firewall Configuration**
   ```bash
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw enable
   ```

### Performance

1. **Scale Workers**
   ```bash
   # Docker
   docker-compose up -d --scale worker=5

   # systemd - create multiple service files
   ```

2. **Database Optimization**
   - Configure MySQL for your workload
   - Add indexes as needed
   - Regular backups

3. **File Cleanup**
   - Enable auto-cleanup in configuration
   - Or create cron job:
   ```bash
   # Delete files older than 7 days
   0 2 * * * find /var/transcription-files -type f -mtime +7 -delete
   ```

### Monitoring

1. **Application Monitoring**
   - Set up logging aggregation (ELK Stack, Grafana Loki)
   - Monitor API response times
   - Track job success/failure rates

2. **System Monitoring**
   - CPU, Memory, Disk usage
   - Network traffic
   - Service uptime

3. **Alerts**
   - Set up alerts for:
     - Service failures
     - High queue length
     - Database issues
     - Disk space

### Backup Strategy

1. **Database Backups**
   ```bash
   # Daily backup cron job
   0 3 * * * mysqldump -u root -p transcription_db > /backup/transcription_db_$(date +\%Y\%m\%d).sql
   ```

2. **File Backups**
   - Transcription files
   - Configuration files
   - Models

### High Availability

1. **Load Balancing**
   - Use Nginx/HAProxy for API load balancing
   - Multiple API instances

2. **Database Replication**
   - MySQL master-slave replication
   - Or use managed database service

3. **Message Queue Clustering**
   - RabbitMQ clustering for high availability

---

## Testing

### Run Unit Tests
```bash
dotnet test
```

### Run E2E Tests
```bash
dotnet test tests/TranscriptionService.E2ETests
```

### Run API Tests
```bash
./scripts/test-api.sh
```

### Manual Testing
```bash
# 1. Submit job
curl -X POST http://localhost:5000/api/transcription/submit \
  -F "AudioFile=@test-audio.wav" \
  -F "Model=Base" \
  -F "Language=en"

# 2. Get status (replace JOB_ID)
curl http://localhost:5000/api/transcription/{JOB_ID}/status

# 3. Get result
curl http://localhost:5000/api/transcription/{JOB_ID}/result
```

---

## Support

For issues, questions, or contributions:
- GitHub Issues: <repository-url>/issues
- Documentation: <repository-url>/wiki
- Email: support@transcriptionservice.com

---

## License

[Your License Here]