#!/bin/bash

PROJECT="src/BrandshareDamSync.Infrastructure.Persistence"
STARTUP="src/BrandshareDamSync.Daemon"

case "$1" in
    add)
        if [ -z "$2" ]; then
            echo "❌ Please provide a migration name."
            exit 1
        fi
        dotnet ef migrations add "$2" --project $PROJECT --startup-project $STARTUP
        ;;
    update)
        dotnet ef database update --project $PROJECT --startup-project $STARTUP
        ;;
    *)
        echo "Usage:"
        echo "  ./migrate.sh add <MigrationName>"
        echo "  ./migrate.sh update"
        ;;
esac
