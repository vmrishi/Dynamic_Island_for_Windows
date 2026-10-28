param([switch]$Run)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build (Join-Path $taskRoot 'source\OrbitIsland.csproj') -c Release --ignore-failed-sources -p:NuGetAudit=false -o (Join-Path $taskRoot 'app')
if($LASTEXITCODE -ne 0){throw 'Build failed. Close Orbit before rebuilding, and check that the .NET 10 SDK is installed.'}
if($Run){Start-Process -FilePath (Join-Path $taskRoot 'app\OrbitIsland.exe') -ArgumentList '--expanded' -WindowStyle Normal}
