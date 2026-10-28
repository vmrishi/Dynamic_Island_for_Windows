param([string]$DataDirectory)
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($DataDirectory)){$DataDirectory=Join-Path $env:TEMP ('OrbitIsland-test-'+[Guid]::NewGuid().ToString('N'))}
$taskTestDirectory=[System.IO.Path]::GetFullPath($DataDirectory)
& dotnet (Join-Path $PSScriptRoot 'app\OrbitIsland.dll') --self-test --data-dir $taskTestDirectory
if($LASTEXITCODE -ne 0){throw 'Self-test process failed.'}
$taskResult=Join-Path $taskTestDirectory 'self-test-results.txt'
if(-not (Test-Path -LiteralPath $taskResult)){throw 'Self-test did not complete.'}
Get-Content -LiteralPath $taskResult
Write-Output ('Test artifacts: '+$taskTestDirectory)
