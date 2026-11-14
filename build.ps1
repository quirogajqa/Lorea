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
Write-Output "Visor creado: $output\$ExecutableName"
