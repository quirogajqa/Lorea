# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Lorea contributors
param([string]$ExecutableName = 'Lorea.exe')
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $framework 'WPF'
$output = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$version = [regex]::Match((Get-Content (Join-Path $PSScriptRoot 'installer.iss') -Raw), '(?m)^#define AppVersion "(\d+\.\d+\.\d+)"').Groups[1].Value
if (-not $version) { throw 'No se encontró la versión de Lorea.' }
$versionSource = Join-Path $output 'Lorea.Version.cs'
Set-Content -LiteralPath $versionSource -Value "[assembly: System.Reflection.AssemblyVersion(`"$version.0`")]" -Encoding ascii
$viewerSources = @('Viewer.cs', 'Updates.cs', 'UpdatesWindow.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu "/out:$output\$ExecutableName" "/win32icon:$PSScriptRoot\assets\lorea.ico" "/resource:$PSScriptRoot\assets\lorea.ico,Lorea.Icon" "/reference:$wpf\PresentationCore.dll" "/reference:$wpf\PresentationFramework.dll" "/reference:$wpf\WindowsBase.dll" /reference:System.Xaml.dll /reference:System.Drawing.dll /reference:Microsoft.VisualBasic.dll /reference:System.Runtime.Serialization.dll @viewerSources $versionSource
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el visor.' }
& (Join-Path $framework 'csc.exe') /nologo /target:library /platform:x64 "/out:$output\Lorea.Thumbnails.dll" "/reference:$wpf\PresentationCore.dll" "/reference:$wpf\PresentationFramework.dll" "/reference:$wpf\WindowsBase.dll" /reference:System.Xaml.dll /reference:System.Drawing.dll /reference:Microsoft.VisualBasic.dll /reference:System.Runtime.Serialization.dll @viewerSources (Join-Path $PSScriptRoot 'SvgThumbnails.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el proveedor de miniaturas.' }
Copy-Item (Join-Path $PSScriptRoot 'thumbnail-policy') $output -Recurse -Force
Write-Output "Visor creado: $output\$ExecutableName"
