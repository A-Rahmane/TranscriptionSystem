#!/bin/bash

set -e

echo "Installing systemd services..."

if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

# Copy service files
cp scripts/systemd/transcription-api.service /etc/systemd/system/
cp scripts/systemd/transcription-worker.service /etc/systemd/system/

# Reload systemd
systemctl daemon-reload

# Enable services
systemctl enable transcription-api.service
systemctl enable transcription-worker.service

echo "✓ Services installed and enabled"
echo ""
echo "To start services:"
echo "  sudo systemctl start transcription-api"
echo "  sudo systemctl start transcription-worker"
echo ""
echo "To view logs:"
echo "  sudo journalctl -u transcription-api -f"
echo "  sudo journalctl -u transcription-worker -f"
