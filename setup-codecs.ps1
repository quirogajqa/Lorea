# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Lorea contributors
# Downloads pinned upstream binaries into the project; never installs system codecs.
$ErrorActionPreference = 'Stop'
$downloads = Join-Path $PSScriptRoot 'tmp\downloads'
$destination = Join-Path $PSScriptRoot 'dist\codecs'
New-Item -ItemType Directory -Force $downloads, $destination | Out-Null
function Get-VerifiedFile([string]$Url, [string]$Target, [string]$Hash) {
    if (-not (Test-Path -LiteralPath $Target)) { Invoke-WebRequest -Uri $Url -OutFile $Target -UseBasicParsing }
    if ((Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -ne $Hash) {
        throw "SHA256 incorrecto: $Target. Descarta la descarga y vuelve a intentarlo."
    }
}
$archive = Join-Path $downloads 'imagemagick-7.1.2-35-q8-x64.7z'
$extractor = Join-Path $downloads '7zr-26.06.exe'
Get-VerifiedFile 'https://github.com/ImageMagick/ImageMagick/releases/download/7.1.2-35/ImageMagick-7.1.2-35-portable-Q8-x64.7z' $archive '287C06F98DDC93800069C124964EE3AD8CEEAF14C107DB22317714A471FF4270'
Get-VerifiedFile 'https://github.com/ip7z/7zip/releases/download/26.06/7zr.exe' $extractor 'E80DB2DAD715A6C5ADABE292D31ED54E11F4E9B99CA349827B50573B175891BC'
& $extractor x $archive "-o$destination" -y
if ($LASTEXITCODE -ne 0) { throw 'No se pudo extraer ImageMagick.' }
Write-Output 'Decodificador preparado en dist\codecs. Conserva LICENSE.txt y NOTICE.txt.'
