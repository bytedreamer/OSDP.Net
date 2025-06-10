#!/bin/bash

# Simple integration test for SimplePDDevice
# This script starts the PD device and attempts to connect with a simple test

echo "Starting SimplePDDevice integration test..."

# Start the SimplePDDevice in background
echo "Starting SimplePDDevice..."
dotnet run &
PD_PID=$!

# Give it time to start
sleep 2

# Test TCP connection
echo "Testing TCP connection to port 4900..."
timeout 3s nc -z localhost 4900
if [ $? -eq 0 ]; then
    echo "✓ TCP port 4900 is accessible"
else
    echo "✗ TCP port 4900 is not accessible"
fi

# Kill the device
echo "Stopping SimplePDDevice..."
kill $PD_PID 2>/dev/null
wait $PD_PID 2>/dev/null

echo "Integration test completed."