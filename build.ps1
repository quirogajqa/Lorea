# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Lorea contributors
param([string]$ExecutableName = 'Lorea.exe')
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $framework 'WPF'
$output = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $output | Out-Null
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu "/out:$output\$ExecutableName" "/win32icon:$PSScriptRoot\assets\lorea.ico" "/resource:$PSScriptRoot\assets\lorea.ico,Lorea.Icon" "/reference:$wpf\PresentationCore.dll" "/reference:$wpf\PresentationFramework.dll" "/reference:$wpf\WindowsBase.dll" /reference:System.Xaml.dll /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'Viewer.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el visor.' }
& (Join-Path $framework 'csc.exe') /nologo /target:library /platform:x64 "/out:$output\Lorea.Thumbnails.dll" "/reference:$wpf\PresentationCore.dll" "/reference:$wpf\PresentationFramework.dll" "/reference:$wpf\WindowsBase.dll" /reference:System.Xaml.dll /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'Viewer.cs') (Join-Path $PSScriptRoot 'SvgThumbnails.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el proveedor de miniaturas.' }
Copy-Item (Join-Path $PSScriptRoot 'thumbnail-policy') $output -Recurse -Force
Write-Output "Visor creado: $output\$ExecutableName"
