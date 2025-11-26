#!/bin/bash

# Script to create a test audio file using ffmpeg

echo "Creating test audio file..."

TESTDATA_DIR="tests/TestData"
mkdir -p "$TESTDATA_DIR"

# Check if ffmpeg is installed
if ! command -v ffmpeg &> /dev/null; then
    echo "ffmpeg is not installed. Installing..."
    sudo apt-get update
    sudo apt-get install -y ffmpeg
fi

# Generate a 5-second test audio file with a 440 Hz tone (A note)
ffmpeg -f lavfi -i "sine=frequency=440:duration=5" \
    -ar 16000 -ac 1 \
    "$TESTDATA_DIR/test-audio.wav" \
    -y

echo "✓ Test audio file created: $TESTDATA_DIR/test-audio.wav"
