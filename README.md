# Lorea

<img src="assets/lorea-logo.png" alt="Logo de Lorea" width="160">

Visor de imágenes para Windows con fondo oscurecido, zoom suave, miniaturas y presentación automática.

**[Descargar el instalador para Windows x64](https://github.com/quirogajqa/Lorea/releases/latest)** · [Licencia GPLv3](LICENSE) · [Contribuir](CONTRIBUTING.md)

Proyecto en desarrollo. El instalador no está firmado digitalmente, por lo que Windows puede mostrar un aviso de editor desconocido.

## Licencia y versiones modificadas

El código propio se publica bajo **GPL-3.0-only**. Consulta [LICENSE](LICENSE), [COPYRIGHT.md](COPYRIGHT.md) y los [avisos de terceros](THIRD_PARTY_NOTICES.md).

**Puedes modificar el programa y compartir tu versión, pero no presentarla como una versión oficial de Lorea.** Identifica tus modificaciones y su fecha, ofrece el código fuente correspondiente según GPLv3 y conserva los avisos. La [política de identidad](BRANDING.md) explica cómo distinguir tu versión del proyecto original sin limitar los permisos de GPLv3.

## Preparar los formatos adicionales

En Windows x64, ejecuta `powershell -ExecutionPolicy Bypass -File .\setup-codecs.ps1`. Descarga los paquetes oficiales fijados por versión y verifica sus SHA256 antes de extraerlos en `dist/codecs`. No instala códecs globales. El compilador requiere .NET Framework 4.8/WPF; el equipo usado para desarrollar Lorea tiene Windows 10 x64.

Visor local para Windows inspirado en la experiencia del visor de Picasa. No modifica las fotos. No está firmado digitalmente.

## Compilar

Ejecuta `powershell -ExecutionPolicy Bypass -File .\build.ps1` en esta carpeta. Utiliza el compilador de .NET Framework de Windows. Para los formatos adicionales se incluye ImageMagick portable en `dist/codecs`; conserva esa carpeta junto al ejecutable. Sus licencias y avisos están en `codecs/LICENSE.txt` y `codecs/NOTICE.txt`.

## Instalar y desinstalar

`dist/installer/Lorea-Setup.exe` instala para el usuario actual en `%LOCALAPPDATA%/Programs/Lorea`, con acceso directo en Inicio y desinstalador en Panel de control > Programas y características. Registra los formatos de imagen en Abrir con y Aplicaciones predeterminadas, sin cambiar automáticamente la selección del usuario. Al terminar, el instalador ofrece abrir los ajustes de aplicaciones predeterminadas. El desinstalador elimina archivos instalados y registros propios, sin borrar fotografías. El instalador no está firmado.

### Miniaturas SVG en el Explorador

El instalador ofrece **Mostrar miniaturas SVG en el Explorador**. El proveedor se registra solo para el usuario actual y respeta otros proveedores existentes. También puede leer vectores Android guardados con extensión `.svg`. No cambia el programa predeterminado ni incluye panel de vista previa o miniaturas de otros formatos.

La extensión implementa `IInitializeWithStream` e `IThumbnailProvider` y mantiene el aislamiento de miniaturas de Windows: no desactiva `DisableProcessIsolation`. Renderiza con límites de tamaño y tiempo; rechaza archivos inválidos y SVG con scripts o recursos externos. Algunos SVG complejos pueden conservar el icono de archivo. Las miniaturas no son animadas. Al desinstalar se elimina únicamente el registro de miniaturas que todavía pertenezca a Lorea. Puedes desactivarlo ejecutando de nuevo el instalador y desmarcando esa opción.

Si una carpeta sigue mostrando iconos antiguos, pulsa F5 o vuelve a abrirla. En Opciones de carpeta > Ver, **Mostrar siempre iconos, nunca vistas en miniatura** debe estar desmarcado. No se fuerza un reinicio del Explorador.

Para reconstruir: `powershell -ExecutionPolicy Bypass -File .\build-installer.ps1`, con Inno Setup disponible en `tmp/inno/ISCC.exe` o indicando `-Compiler`. Incluye solo el decodificador necesario y sus configuraciones y licencias, no los ejecutables de pruebas.

El instalador incluye `COPYING.txt`, la política de identidad, los avisos de terceros y `source/Lorea-source.zip` con el código propio y scripts correspondientes. Puedes generar el ZIP por separado con `package-source.ps1`. No contiene fotos personales, capturas, grabaciones ni los binarios de las dependencias. Para una publicación, conserva juntos el instalador y el código fuente de esa versión; las dependencias y las obligaciones de sus licencias se revisan por separado.

## Abrir fotos

Abre `dist\Lorea.exe` y pulsa **Abrir**, o arrastra una imagen a la ventana. También acepta una ruta como argumento y puedes arrastrar una foto al ejecutable en el Explorador.

Al abrir o cambiar de foto se respeta el tamaño original, sin ampliar las imágenes pequeñas. Las imágenes más grandes que el área disponible se reducen para verlas completas. El botón Ajustar sigue permitiendo llenar el área disponible.

Durante la sesión, cada foto recuerda su zoom y desplazamiento. Al volver a una foto visitada, se restaura la vista que dejaste; también se conserva al pasar entre los modos ventana y sin bordes. Estos datos no se escriben en las fotos ni se conservan tras cerrar el visor.

- Flechas: foto anterior/siguiente de la carpeta, por nombre.
- Play/Pausa: presentación automática cada 3 segundos con desvanecido de 450 ms. Al llegar al final vuelve a la primera foto; requiere al menos dos fotos.
- Rueda: zoom. Arrastrar la foto: moverla.
- Flecha arriba: acercar. Flecha abajo: alejar.
- El zoom tiene una transición de 320 ms. Cada pulsación o repetición cambia el tamaño un 10%. Un indicador temporal en el centro muestra el porcentaje animado respecto al ancho original de la imagen (en unidades de pantalla de WPF), siguiendo el tamaño visible durante toda la transición.
- Doble clic, Inicio o 0: ajustar.
- Ctrl+O: abrir. Esc: cerrar.
- Clic fuera de la foto: pasar del escritorio oscurecido a una ventana normal, que puedes mover, redimensionar, minimizar y maximizar. En modo ventana, el clic en el fondo no cierra el visor.
- El botón Maximizar de la ventana vuelve al modo inicial sin bordes, con el escritorio oscurecido y la misma foto abierta.
- En modo sin bordes, la cruz superior derecha aparece y se oculta junto con los controles. Pasar el mouse por esa esquina también muestra los controles; la cruz cierra el visor. En ventana se usa el botón de cierre normal de Windows.
- La franja inferior flota sobre la imagen y se oculta tras 2 segundos; reaparece al pasar el mouse por la zona inferior y permanece visible mientras la usas. La foto aprovecha toda el área disponible, sin reservar una franja para los controles ni cambiar de tamaño cuando aparecen.

La tira inferior muestra hasta 21 miniaturas alrededor de la foto actual, cargadas en segundo plano. Haz clic en una para abrirla; las flechas permiten seguir recorriendo toda la carpeta.

Formatos: JPG/JPEG/JFIF, PNG/APNG, BMP, GIF, TIFF, WebP, HEIC/HEIF, AVIF, SVG/SVGZ, RAW de cámaras (DNG, CR2/CR3, NEF, ARW, RAF, ORF, RW2 y otros), PSD/PSB, ICO/CUR, TGA, PCX, DDS, EXR/HDR, JPEG 2000, JPEG XL, QOI, PNM y otros formatos de imagen. Las variantes concretas de RAW dependen de LibRaw; los archivos `.raw` sin cabecera ni dimensiones pueden no ser decodificables. No admite PDF ni videos. Los GIF se reproducen animados; otras animaciones, archivos multipágina y documentos por capas se muestran como una imagen fija (primer fotograma o imagen compuesta).

La conversión adicional es local y no modifica originales. La primera apertura puede tardar más; el PNG de lectura queda en `%TEMP%/Lorea-image-cache` y se reutiliza. Al iniciar se limpian los PNG propios de más de 7 días y el exceso sobre 512 MiB. La caché de bitmaps retiene hasta 8 imágenes y 128 MiB estimados (no es un límite de memoria total del proceso). Las fotos vecinas se preparan en segundo plano. La vista inicial usa hasta 1920 píxeles de ancho y el zoom solicita más detalle según el tamaño mostrado y la densidad de pantalla, con límite de 24 megapíxeles por bitmap. Las imágenes mayores pueden no alcanzar resolución original completa. GIF sigue un camino de reproducción separado.

Decodificador: ImageMagick 7.1.2-35 portable Q8 x64, obtenido del lanzamiento oficial de GitHub. SHA256 del archivo 7z: `287C06F98DDC93800069C124964EE3AD8CEEAF14C107DB22317714A471FF4270`. Conserva los archivos de configuración y licencias de la distribución portable.

Validación: conversión y lectura WPF de muestras WebP, AVIF, SVG, HEIC/HEIF, Canon CR2, PSD, ICO, QOI, JP2, JXL y TGA. HEIC de `strukturag/libheif/examples/example.heic`; CR2 de `letmaik/rawpy/test/RAW_CANON_40D_SRAW_V103.CR2`. No se han probado todas las cámaras ni variantes.
