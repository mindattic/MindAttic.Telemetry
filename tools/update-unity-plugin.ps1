# Rebuilds the core project's netstandard2.1 target in Release and copies the resulting .dll/.xml into
# unity/com.mindattic.telemetry/Runtime/Plugins. Run after changing src/MindAttic.Telemetry; every Unity
# project references the package by path and picks it up on focus.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
dotnet build "$root\src\MindAttic.Telemetry\MindAttic.Telemetry.csproj" -c Release -f netstandard2.1
$src = "$root\src\MindAttic.Telemetry\bin\Release\netstandard2.1"
$dst = "$root\unity\com.mindattic.telemetry\Runtime\Plugins"
Copy-Item "$src\MindAttic.Telemetry.dll", "$src\MindAttic.Telemetry.xml" $dst -Force
Write-Host "Updated $dst"
