#!/bin/bash
# Send notification
curl -X POST https://example.com/notify \
  -H "Content-Type: application/json" \
  -d '{"message":"Deployment started"}'
