#!/bin/bash

# Script to stop all running services

echo "Stopping Transcription Services..."

# Find and kill API processes
API_PIDS=$(pgrep -f "TranscriptionService.API")
if [ -n "$API_PIDS" ]; then
    echo "Stopping API Service (PIDs: $API_PIDS)..."
    kill $API_PIDS
fi

# Find and kill Worker processes
WORKER_PIDS=$(pgrep -f "TranscriptionService.Worker")
if [ -n "$WORKER_PIDS" ]; then
    echo "Stopping Worker Service (PIDs: $WORKER_PIDS)..."
    kill $WORKER_PIDS
fi

echo "All services stopped"
