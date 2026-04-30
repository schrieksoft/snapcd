#!/bin/bash
# Check required environment variables
if [ -z "$WORKSPACE" ]; then
  echo "ERROR: WORKSPACE not set"
  exit 1
fi
echo "Environment check passed"
