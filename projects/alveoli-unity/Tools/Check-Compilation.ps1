$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Data'
if (-not (Test-Path -LiteralPath $unityData)) { throw 'Unity 6000.3.27f1 installation not found. Update $unityData in this script.' }
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
$compileArgs = @('-nologo', '-target:library', '-nostdlib+', ('-out:' + (Join-Path $project 'Logs/Alveoli.Compilation.Check.dll')))
$compileArgs += Get-ChildItem "$unityData/NetStandard/ref/2.1.0" -Filter *.dll | ForEach-Object { '-r:' + $_.FullName }
$compileArgs += Get-ChildItem "$unityData/Managed/UnityEngine" -Filter *.dll | ForEach-Object { '-r:' + $_.FullName }
$compileArgs += Get-ChildItem (Join-Path $project 'Assets/LungGasExchange') -Filter *.cs -Recurse | ForEach-Object { $_.FullName }
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" @compileArgs
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' }
Write-Output 'PASS: Runtime and editor scripts compile against installed Unity assemblies. This does not validate package import, shaders or rendered output.'
