#!/bin/bash

set -e

echo "╔══════════════════════════════════════════════════╗"
echo "║      Whisper Models Download Script              ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

MODELS_DIR="/var/models/whisper"
BASE_URL="https://huggingface.co/ggerganov/whisper.cpp/resolve/main"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

# Create models directory
mkdir -p "$MODELS_DIR"

# Function to download model
download_model() {
    local model_name=$1
    local file_name="ggml-${model_name}.bin"
    local url="${BASE_URL}/${file_name}"
    local output_path="${MODELS_DIR}/${file_name}"
    
    if [ -f "$output_path" ]; then
        echo "Model $model_name already exists. Skipping..."
        return
    fi
    
    echo "Downloading $model_name model..."
    wget -O "$output_path" "$url" --progress=bar:force 2>&1 | tail -n 5
    
    if [ -f "$output_path" ]; then
        local size=$(du -h "$output_path" | cut -f1)
        echo "✓ Downloaded $model_name ($size)"
    else
        echo "✗ Failed to download $model_name"
    fi
}

echo "Select models to download:"
echo "1) Tiny (~75 MB) - Fastest, least accurate"
echo "2) Base (~142 MB) - Fast, moderate accuracy"
echo "3) Small (~466 MB) - Balanced"
echo "4) Medium (~1.5 GB) - High accuracy"
echo "5) Large (~2.9 GB) - Highest accuracy"
echo "6) All models"
echo "7) Recommended (Base + Small)"
echo ""
read -p "Enter your choice (1-7): " choice

case $choice in
    1)
        download_model "tiny"
        ;;
    2)
        download_model "base"
        ;;
    3)
        download_model "small"
        ;;
    4)
        download_model "medium"
        ;;
    5)
        download_model "large-v3"
        ;;
    6)
        download_model "tiny"
        download_model "base"
        download_model "small"
        download_model "medium"
        download_model "large-v3"
        ;;
    7)
        download_model "base"
        download_model "small"
        ;;
    *)
        echo "Invalid choice"
        exit 1
        ;;
esac

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         Download Complete!                       ║"
echo "╠══════════════════════════════════════════════════╣"
echo "║ Models location: $MODELS_DIR"
echo "║                                                  ║"
echo "║ Available models:"
ls -lh "$MODELS_DIR" | grep "ggml-" | awk '{print "║   - " $9 " (" $5 ")"}'
echo "╚══════════════════════════════════════════════════╝"
