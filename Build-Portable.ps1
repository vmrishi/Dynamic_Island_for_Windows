$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$output=Join-Path $taskRoot 'portable-output\Orbit-Island-Portable-v0.4'
if(Test-Path $output){Remove-Item $output -Recurse -Force}
dotnet publish (Join-Path $taskRoot 'source\OrbitIsland.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:NuGetAudit=false -o $output
if($LASTEXITCODE -ne 0){throw 'Portable build failed. Install the .NET 10 SDK and retry.'}
New-Item (Join-Path $output 'portable.mode') -ItemType File -Force | Out-Null
Copy-Item (Join-Path $taskRoot 'source\Broker.ps1') $output -Force
foreach($name in @('README.md','VALIDATION.txt','LICENSE','Capture-UI.ps1')){Copy-Item (Join-Path $taskRoot $name) $output -Force}
New-Item (Join-Path $output 'Licenses') -ItemType Directory -Force | Out-Null
Copy-Item (Join-Path $taskRoot 'source\Assets\Lucide-LICENSE.txt') (Join-Path $output 'Licenses') -Force
$zip=Join-Path $taskRoot 'downloads\Orbit-Island-Portable-v0.4-win-x64.zip'
New-Item (Split-Path $zip) -ItemType Directory -Force | Out-Null
Compress-Archive -Path $output -DestinationPath $zip -Force
Write-Host "Portable ZIP: $zip"
