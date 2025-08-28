param(
    [string]$cmd = "",
    [string]$name = ""
)

$project = "src/BrandShareDAMSync.Infrastructure.Persistence"
$startup = "src/BrandShareDAMSync.Daemon"

switch ($cmd) {
    "add" {
        if (-not $name) {
            Write-Host "❌ Please provide a migration name."
            exit 1
        }
        dotnet ef migrations add $name --project $project --startup-project $startup
    }
    "update" {
        dotnet ef database update --project $project --startup-project $startup
    }
    default {
        Write-Host "Usage:"
        Write-Host "  ./migrate.ps1 add <MigrationName>"
        Write-Host "  ./migrate.ps1 update"
    }
}
