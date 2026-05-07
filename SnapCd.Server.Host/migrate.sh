# Ensure an argument is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <MigrationName>"
    exit 1
fi

MIGRATION_NAME=$1

dotnet ef migrations add $MIGRATION_NAME --startup-project . --project ../SnapCd.Server.Common --context SnapCdDbContext --output-dir Database/Migrations
