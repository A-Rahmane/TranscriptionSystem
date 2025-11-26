#!/bin/bash

# Script to run both API and Worker services

echo "Starting Transcription Services..."
echo "=================================="

# Set environment
export ASPNETCORE_ENVIRONMENT=Development

# Start API in background
echo "Starting API Service..."
cd src/TranscriptionService.API
dotnet run &
API_PID=$!
cd ../..

# Wait a bit for API to start
sleep 3

# Start Worker in background
echo "Starting Worker Service..."
cd src/TranscriptionService.Worker
dotnet run &
WORKER_PID=$!
cd ../..

echo ""
echo "Services started:"
echo "  API PID: $API_PID"
echo "  Worker PID: $WORKER_PID"
echo ""
echo "Press Ctrl+C to stop all services"
echo ""

# Handle shutdown
trap "echo 'Stopping services...'; kill $API_PID $WORKER_PID; exit" INT TERM

# Wait for processes
wait $API_PID $WORKER_PID
