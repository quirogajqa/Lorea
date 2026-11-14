# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Lorea contributors
param([switch]$ShellIntegration)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$fixtures = Join-Path $root 'tmp\thumbnail-tests'
New-Item -ItemType Directory -Force $fixtures | Out-Null
'<svg xmlns="http://www.w3.org/2000/svg" width="96" height="64"><rect width="96" height="64" fill="#2896c8"/></svg>' | Set-Content (Join-Path $fixtures 'valid.svg')
'<!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"><svg xmlns="http://www.w3.org/2000/svg" width="96" height="64"><rect width="96" height="64" fill="red"/></svg>' | Set-Content (Join-Path $fixtures 'doctype.svg')
'<vector xmlns:android="http://schemas.android.com/apk/res/android" android:viewportWidth="24" android:viewportHeight="24"><path android:pathData="M0,0 L24,0 L24,24 L0,24 Z" android:fillColor="#ff0000"/></vector>' | Set-Content (Join-Path $fixtures 'android.svg')
'<svg broken' | Set-Content (Join-Path $fixtures 'invalid.svg')
'<svg xmlns="http://www.w3.org/2000/svg"><image href="https://example.com/image.png"/></svg>' | Set-Content (Join-Path $fixtures 'external.svg')
& "$framework\csc.exe" /nologo /platform:x64 "/out:$root\dist\ThumbnailSmoke.exe" "/reference:$root\dist\Lorea.Thumbnails.dll" (Join-Path $PSScriptRoot 'ThumbnailSmoke.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
& "$root\dist\ThumbnailSmoke.exe" (Join-Path $fixtures 'valid.svg') (Join-Path $fixtures 'doctype.svg') (Join-Path $fixtures 'android.svg') ('reject:' + (Join-Path $fixtures 'invalid.svg')) ('reject:' + (Join-Path $fixtures 'external.svg'))
if ($LASTEXITCODE -ne 0) { throw 'Thumbnail component tests failed.' }
if ($ShellIntegration) {
    & "$framework\csc.exe" /nologo /platform:x64 "/out:$root\dist\ShellThumbnailSmoke.exe" (Join-Path $PSScriptRoot 'ShellThumbnailSmoke.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
    & "$root\dist\ShellThumbnailSmoke.exe" (Join-Path $fixtures 'valid.svg') (Join-Path $fixtures 'android.svg')
    if ($LASTEXITCODE -ne 0) { throw 'Windows Shell thumbnail test failed. Install and enable the provider first.' }
}
