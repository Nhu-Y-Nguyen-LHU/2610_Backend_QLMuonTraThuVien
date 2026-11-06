[CmdletBinding()]
param(
    [string]$Url = "http://localhost:5187",
    [switch]$StopServer
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\LibraryAPI\LibraryAPI.csproj"
$dll = Join-Path $root "src\LibraryAPI\bin\Debug\net8.0\LibraryAPI.dll"
$logDirectory = Join-Path $root "src\LibraryAPI\logs"
$serverOutputLog = Join-Path $logDirectory "development-server.out.log"
$serverErrorLog = Join-Path $logDirectory "development-server.err.log"

function Get-PortProcessId([int]$Port) {
    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($connection) { return $connection.OwningProcess }
    return $null
}

function Wait-ForApi([string]$Address) {
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$Address/" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    throw "API did not become ready at $Address within 30 seconds."
}

Set-Location $root
Write-Host "Building LibraryAPI..." -ForegroundColor Cyan
dotnet build $project --no-restore
if (-not $?) { throw "Build failed." }

$uri = [Uri]$Url
$portProcessId = Get-PortProcessId $uri.Port
if ($portProcessId) {
    Write-Host "An API is already listening on port $($uri.Port) (PID $portProcessId)." -ForegroundColor Yellow
} else {
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    Write-Host "Starting API at $Url..." -ForegroundColor Cyan
    $server = Start-Process -FilePath "dotnet" `
        -ArgumentList "`"$dll`"", "--urls", $Url `
        -WorkingDirectory (Join-Path $root "src\LibraryAPI") `
        -RedirectStandardOutput $serverOutputLog `
        -RedirectStandardError $serverErrorLog `
        -PassThru
    Write-Host "API process started with PID $($server.Id)." -ForegroundColor DarkGray
}

Wait-ForApi $Url
$dashboard = Invoke-WebRequest -Uri "$Url/" -UseBasicParsing
$books = Invoke-WebRequest -Uri "$Url/api/books" -UseBasicParsing
if ($dashboard.StatusCode -ne 200 -or $books.StatusCode -ne 200) {
    throw "Smoke tests failed: dashboard=$($dashboard.StatusCode), books=$($books.StatusCode)."
}

Write-Host "Smoke tests passed: dashboard HTTP $($dashboard.StatusCode), books HTTP $($books.StatusCode)." -ForegroundColor Green
Write-Host "Opening browser: $Url" -ForegroundColor Cyan
Start-Process $Url
Write-Host "Use Ctrl+C only when running interactively; the API process remains available for browser testing." -ForegroundColor DarkGray

if ($StopServer -and $server) {
    Stop-Process -Id $server.Id
    Write-Host "API process stopped." -ForegroundColor DarkGray
}
