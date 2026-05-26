# Pokrece sve SQL migracije (1 servis = 1 baza).
# Primer: .\Run-Migrations.ps1
# Opciono: .\Run-Migrations.ps1 -Server "localhost,1434" -Password "Str0ng!Pass123"

param(
    [string]$Server = "localhost,1434",
    [string]$User = "sa",
    [string]$Password = "Str0ng!Pass123"
)

$ErrorActionPreference = "Stop"
$migrationsDir = $PSScriptRoot
$masterScript = Join-Path $migrationsDir "RunAll_PerServiceDatabases.sql"

if (-not (Test-Path $masterScript)) {
    throw "Nije pronadjen: $masterScript"
}

Write-Host "Migracije iz: $migrationsDir"
Write-Host "SQL Server: $Server"

Push-Location $migrationsDir
try {
    sqlcmd -S $Server -U $User -P $Password -C -b -i $masterScript
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd zavrsio sa kodom $LASTEXITCODE"
    }
    Write-Host "Gotovo. Osvezi Databases u SSMS (Refresh)."
}
finally {
    Pop-Location
}
