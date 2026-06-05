#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <MigrationName>"
    exit 1
fi

MIGRATION_NAME=$1

# Run from this directory. The Self-Hosted DbContext (SelfHostedSnapCdDbContext)
# lives in this project, so startup-project and project are the same — both
# default to '.' when omitted.
dotnet ef migrations add "$MIGRATION_NAME" \
    --context SelfHostedSnapCdDbContext \
    --output-dir Database/Migrations
