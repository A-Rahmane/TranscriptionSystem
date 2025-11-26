#!/bin/bash

set -e

echo "╔══════════════════════════════════════════════════╗"
echo "║   Whisper.cpp Installation Script for Ubuntu    ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# Configuration
WHISPER_DIR="/opt/whisper.cpp"
INSTALL_DIR="/usr/local/bin"
MODELS_DIR="/var/models/whisper"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

echo "Step 1: Installing system dependencies..."
apt-get update
apt-get install -y \
    build-essential \
    git \
    cmake \
    libsdl2-dev \
    wget \
    curl

echo ""
echo "Step 2: Cloning Whisper.cpp repository..."
if [ -d "$WHISPER_DIR" ]; then
    echo "Directory $WHISPER_DIR already exists. Updating..."
    cd "$WHISPER_DIR"
    git pull
else
    git clone https://github.com/ggerganov/whisper.cpp.git "$WHISPER_DIR"
    cd "$WHISPER_DIR"
fi

echo ""
echo "Step 3: Building Whisper.cpp..."
make clean
make

echo ""
echo "Step 4: Installing binary to $INSTALL_DIR..."
cp main "$INSTALL_DIR/whisper"
chmod +x "$INSTALL_DIR/whisper"

echo ""
echo "Step 5: Creating models directory..."
mkdir -p "$MODELS_DIR"

echo ""
echo "Step 6: Verifying installation..."
if [ -f "$INSTALL_DIR/whisper" ]; then
    echo "✓ Whisper.cpp installed successfully!"
    "$INSTALL_DIR/whisper" --help | head -n 5
else
    echo "✗ Installation failed!"
    exit 1
fi

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         Installation Complete!                   ║"
echo "╠══════════════════════════════════════════════════╣"
echo "║ Binary location: $INSTALL_DIR/whisper"
echo "║ Models directory: $MODELS_DIR"
echo "║                                                  ║"
echo "║ Next step: Download models using:               ║"
echo "║   sudo ./scripts/download-models.sh              ║"
echo "╚══════════════════════════════════════════════════╝"
