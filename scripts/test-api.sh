#!/bin/bash

set -e

echo "╔══════════════════════════════════════════════════╗"
echo "║      Transcription Service API Test              ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

API_URL="${API_URL:-http://localhost:5000}"
TEST_FILE="${TEST_FILE:-tests/TestData/test-audio.wav}"

echo "API URL: $API_URL"
echo "Test file: $TEST_FILE"
echo ""

# Check if test file exists
if [ ! -f "$TEST_FILE" ]; then
    echo "Test file not found. Creating test audio..."
    ./scripts/create-test-audio.sh
fi

echo "Test 1: Health Check"
echo "--------------------"
response=$(curl -s -w "\n%{http_code}" "$API_URL/health")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n-1)

if [ "$http_code" -eq 200 ]; then
    echo "✓ Health check passed"
    echo "  Response: $body"
else
    echo "✗ Health check failed (HTTP $http_code)"
    exit 1
fi

echo ""
echo "Test 2: Submit Transcription Job"
echo "---------------------------------"
response=$(curl -s -w "\n%{http_code}" \
    -F "AudioFile=@$TEST_FILE" \
    -F "Model=Base" \
    -F "Language=en" \
    "$API_URL/api/transcription/submit")

http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n-1)

if [ "$http_code" -eq 200 ]; then
    echo "✓ Job submitted successfully"
    echo "  Response: $body"
    
    # Extract job ID
    JOB_ID=$(echo "$body" | grep -o '"jobId":"[^"]*"' | cut -d'"' -f4)
    echo "  Job ID: $JOB_ID"
else
    echo "✗ Job submission failed (HTTP $http_code)"
    echo "  Response: $body"
    exit 1
fi

if [ -z "$JOB_ID" ]; then
    echo "✗ Could not extract job ID"
    exit 1
fi

echo ""
echo "Test 3: Get Job Status"
echo "----------------------"
response=$(curl -s -w "\n%{http_code}" "$API_URL/api/transcription/$JOB_ID/status")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n-1)

if [ "$http_code" -eq 200 ]; then
    echo "✓ Job status retrieved"
    echo "  Response: $body"
else
    echo "✗ Failed to get job status (HTTP $http_code)"
    exit 1
fi

echo ""
echo "Test 4: Wait for Job Completion"
echo "--------------------------------"
echo "Waiting for job to complete (max 60 seconds)..."

for i in {1..12}; do
    sleep 5
    response=(curl−s"(curl -s "
(curl−s"API_URL/api/transcription/$JOB_ID/status")
    status=(echo"(echo "
(echo"response" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)

echo "  Attempt $i/12: Status = $status"

if [ "$status" = "Completed" ]; then
    echo "✓ Job completed successfully!"
    break
elif [ "$status" = "Failed" ]; then
    echo "✗ Job failed"
    echo "  Response: $response"
    exit 1
fi

if [ $i -eq 12 ]; then
    echo "✗ Job did not complete within timeout"
    exit 1
fi

done
echo ""
echo "Test 5: Get Transcription Result"
echo "---------------------------------"
response=(curl−s−w"\n(curl -s -w "\n%{http_code}" "
(curl−s−w"\nAPI_URL/api/transcription/$JOB_ID/result")
http_code=(echo"(echo "
(echo"response" | tail -n1)
body=(echo"(echo "
(echo"response" | head -n-1)

if [ "$http_code" -eq 200 ]; then
echo "✓ Transcription result retrieved"
echo "  Response: $body"
else
echo "✗ Failed to get result (HTTP $http_code)"
exit 1
fi
echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         All Tests Passed! ✓                      ║"
echo "╚══════════════════════════════════════════════════╝"
