#!/bin/bash

set -e

echo "╔══════════════════════════════════════════════════╗"
echo "║  Transcription Service - Ubuntu Setup Script    ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

echo "This script will install and configure:"
echo "  - .NET 8 SDK"
echo "  - MySQL 8.0"
echo "  - RabbitMQ"
echo "  - Whisper.cpp"
echo "  - All required dependencies"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
fi

echo ""
echo "Step 1: Updating system packages..."
apt-get update
apt-get upgrade -y

echo ""
echo "Step 2: Installing .NET 8 SDK..."
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt-get update
apt-get install -y dotnet-sdk-8.0

# Verify .NET installation
dotnet --version

echo ""
echo "Step 3: Installing MySQL..."
apt-get install -y mysql-server

# Start MySQL service
systemctl start mysql
systemctl enable mysql

echo ""
echo "Step 4: Installing RabbitMQ..."
apt-get install -y rabbitmq-server

# Start RabbitMQ service
systemctl start rabbitmq-server
systemctl enable rabbitmq-server

# Enable management plugin
rabbitmq-plugins enable rabbitmq_management

echo ""
echo "Step 5: Installing Whisper.cpp..."
./scripts/install-whisper-cpp.sh

echo ""
echo "Step 6: Creating application directories..."
mkdir -p /var/transcription-files
chmod 755 /var/transcription-files

mkdir -p /var/models/whisper
chmod 755 /var/models/whisper

echo ""
echo "Step 7: Configuring MySQL..."
echo "Creating database and user..."

mysql -u root <<MYSQL_SCRIPT
CREATE DATABASE IF NOT EXISTS transcription_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'transcription_user'@'localhost' IDENTIFIED BY 'transcription_pass_2024';
GRANT ALL PRIVILEGES ON transcription_db.* TO 'transcription_user'@'localhost';
FLUSH PRIVILEGES;
MYSQL_SCRIPT

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         Setup Complete!                          ║"
echo "╠══════════════════════════════════════════════════╣"
echo "║ Next steps:                                      ║"
echo "║                                                  ║"
echo "║ 1. Download Whisper models:                     ║"
echo "║    sudo ./scripts/download-models.sh             ║"
echo "║                                                  ║"
echo "║ 2. Update appsettings.json with:                ║"
echo "║    - Database connection string                 ║"
echo "║    - RabbitMQ credentials                       ║"
echo "║                                                  ║"
echo "║ 3. Build the solution:                          ║"
echo "║    dotnet build                                  ║"
echo "║                                                  ║"
echo "║ 4. Run migrations:                              ║"
echo "║    cd src/TranscriptionService.API              ║"
echo "║    dotnet ef database update                     ║"
echo "║                                                  ║"
echo "║ 5. Start services:                              ║"
echo "║    ./scripts/run-services.sh                     ║"
echo "║                                                  ║"
echo "║ RabbitMQ Management: http://localhost:15672     ║"
echo "║ (guest/guest)                                    ║"
echo "╚══════════════════════════════════════════════════╝"
