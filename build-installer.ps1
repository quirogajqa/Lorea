# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Lorea contributors
param([string]$Compiler = "$PSScriptRoot\tmp\inno\ISCC.exe")
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\build.ps1" -ExecutableName 'Lorea-release.exe'
& "$PSScriptRoot\package-source.ps1"
$source = Get-Content (Join-Path $PSScriptRoot 'Viewer.cs') -Raw
$match = [regex]::Match($source, 'static readonly string\[\] extensions = \("([^"]+)"\)')
if (-not $match.Success) { throw 'No se encontró la lista de formatos.' }
$extensions = $match.Groups[1].Value.Split(' ') | Where-Object { $_.StartsWith('.') }
$lines = foreach ($extension in $extensions) {
    'Root: HKCU; Subkey: "Software\Classes\' + $extension + '\OpenWithProgids"; ValueType: none; ValueName: "Piccasa.Image"; Flags: uninsdeletevalue'
    'Root: HKCU; Subkey: "Software\Piccasa\Capabilities\FileAssociations"; ValueType: string; ValueName: "' + $extension + '"; ValueData: "Piccasa.Image"'
    'Root: HKCU; Subkey: "Software\Classes\Applications\Lorea.exe\SupportedTypes"; ValueType: string; ValueName: "' + $extension + '"; ValueData: ""'
}
New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'tmp') | Out-Null
$lines | Set-Content (Join-Path $PSScriptRoot 'tmp\associations.iss') -Encoding utf8
& $Compiler (Join-Path $PSScriptRoot 'installer.iss')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el instalador.' }
