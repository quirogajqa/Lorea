# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Lorea contributors
$ErrorActionPreference = 'Stop'
# Explicit allowlist: no user photos, local configuration, credentials or build artifacts.
$files = @('Viewer.cs','SvgThumbnails.cs','thumbnail-policy','tests','build.ps1','build-installer.ps1','setup-codecs.ps1','package-source.ps1','installer.iss','README.md','LICENSE','COPYRIGHT.md','BRANDING.md','THIRD_PARTY_NOTICES.md','CONTRIBUTING.md','RELEASE_NOTES.md','.gitignore','assets')
$output = Join-Path $PSScriptRoot 'dist\source'
New-Item -ItemType Directory -Force $output | Out-Null
$archive = Join-Path $output 'Lorea-source.zip'
$paths = $files | ForEach-Object { Join-Path $PSScriptRoot $_ }
Compress-Archive -LiteralPath $paths -DestinationPath $archive -Force
Write-Output "Código preparado: $archive"
