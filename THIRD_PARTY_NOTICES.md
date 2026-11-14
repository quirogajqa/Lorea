# Dependencias y atribuciones

GPL-3.0-only cubre el código propio de Lorea. Las dependencias conservan sus licencias y avisos originales.

## ImageMagick

Se usa ImageMagick 7.1.2-35 portable Q8 x64 como proceso separado para decodificar formatos adicionales. Copyright ImageMagick Studio LLC. No está modificado, salvo que Lorea puede generar un SVG temporal compatible antes de invocarlo.

- Fuentes y distribución: https://github.com/ImageMagick/ImageMagick/releases/tag/7.1.2-35
- Licencia: https://imagemagick.org/license/
- El paquete incorpora bibliotecas de terceros con avisos en su `NOTICE.txt`.
- Los instaladores de Lorea deben incluir íntegros `codecs/LICENSE.txt` y `codecs/NOTICE.txt`. No sustituyas esos avisos por MIT.

## 7-Zip

`setup-codecs.ps1` descarga el extractor oficial `7zr.exe` 26.06, distribuido como dominio público según su banner. Es una herramienta de preparación: no se incluye en el instalador de Lorea.

Fuente: https://github.com/ip7z/7zip/releases/tag/26.06

## Windows y herramientas de compilación

Lorea usa .NET Framework/WPF de Windows. El instalador se construye con Inno Setup, que se obtiene por separado y conserva sus términos propios: https://jrsoftware.org/inno-setup

## Identidad visual y referencias

El logo del loro se generó con image_gen y fue aprobado por el creador de Lorea. Se incluyen el PNG original y su conversión a ICO; consulta `assets/README.md`.

Lorea es un proyecto independiente inspirado en la experiencia del visor de Picasa. No incluye código, instaladores, iconos ni otros recursos de Picasa o Google, ni implica afiliación con ellos.

No se incluyen fotografías personales, capturas, grabaciones ni muestras RAW de terceros en el paquete de código fuente.
