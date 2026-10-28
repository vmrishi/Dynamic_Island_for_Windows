$ErrorActionPreference='Stop'
$exe=Join-Path $PSScriptRoot 'OrbitIsland.exe'
if(-not (Test-Path $exe)){$exe=Join-Path $PSScriptRoot 'app\OrbitIsland.exe'}
if(-not (Test-Path $exe)){throw 'Build the app or place this script beside the portable OrbitIsland.exe.'}
$started=Get-Date
Start-Process -FilePath $exe -ArgumentList '--visual-test' -Wait
$output=Get-ChildItem -Path $env:TEMP -Directory -Filter 'OrbitVisualTest-*' | Where-Object {$_.CreationTime -ge $started.AddSeconds(-2)} | Sort-Object CreationTime -Descending | Select-Object -First 1
if($null -eq $output){throw 'No visual-test output found.'}
$result=Join-Path $output.FullName 'result.txt'
if(-not (Test-Path $result)){throw ('Visual test did not finish; inspect '+$output.FullName)}
Get-Content $result
if((Get-Content $result -Raw).Trim() -ne 'PASS'){throw ('Visual checks failed; inspect '+$output.FullName)}
Write-Host ('Screenshots: '+(Join-Path $output.FullName 'screenshots'))
Start-Process explorer.exe -ArgumentList (Join-Path $output.FullName 'screenshots')
