#!/bin/env pwsh

Push-Location $PSScriptRoot

$runtimes = @(
    "win10-x64",
    "osx-x64",
    "osx.11.0-arm64",
    "linux-x64",
    "linux-musl-x64",
    "linux-arm64"
)

$runtimes | ForEach-Object { dotnet publish . -c Release -o out/$_ --runtime $_ }

Pop-Location