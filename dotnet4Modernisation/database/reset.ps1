# Rebuild both databases. Run this on Windows, from this folder:
#   powershell -ExecutionPolicy Bypass -File .\reset.ps1
# Optional: $env:APEX_SQL_SERVER = ".\SQLEXPRESS"

$ErrorActionPreference = "Stop"
$server = if ($env:APEX_SQL_SERVER) { $env:APEX_SQL_SERVER } else { "(localdb)\MSSQLLocalDB" }

if ($server -eq "(localdb)\MSSQLLocalDB") {
    $localdb = Get-Command sqllocaldb -ErrorAction SilentlyContinue
    if ($localdb) {
        & $localdb.Source start MSSQLLocalDB | Out-Null
    }
}

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Error "sqlcmd was not found. Install SQL Server Express LocalDB (Visual Studio) or run the three .sql files in SSMS in order."
}

function Invoke-SqlFile([string]$file, [string]$database) {
    Write-Host "Running $file"
    if ($database) {
        & $sqlcmd.Source -S $server -E -b -d $database -i $file
    } else {
        & $sqlcmd.Source -S $server -E -b -i $file
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Error "sqlcmd failed on $file (exit $LASTEXITCODE)"
    }
}

$dir = $PSScriptRoot
Invoke-SqlFile (Join-Path $dir "01_schema.sql") $null
Invoke-SqlFile (Join-Path $dir "02_seed.sql") "ApexCounter"
Invoke-SqlFile (Join-Path $dir "03_modern_copy.sql") "master"
Write-Host "Ready. Legacy database ApexCounter, modern copy ApexCounter_Modern, server $server"
