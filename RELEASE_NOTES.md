# Lorea 0.1.10

- **Actualizaciones** muestra la versión instalada, las notas de la nueva versión y el progreso de descarga. Puedes seguir usando el visor y elegir cuándo instalar.
- Antes de abrir el instalador, Lorea verifica su SHA-256. Si falla la búsqueda o la descarga, puedes reintentarlo desde la misma ventana.
- Lorea se vuelve a abrir cuando la instalación termina correctamente.
- **Copiar foto** en el menú contextual copia la imagen al portapapeles. **Suprimir** pide confirmación antes de enviar la foto abierta a la Papelera.

# Lorea 0.1.8

## Corrección de varios monitores

Al mover la ventana a otra pantalla y maximizar, el visor sin bordes permanece en ese monitor. Al volver a ventana también se conserva la pantalla. Verificado con una prueba de integración de dos monitores.

## Miniaturas SVG en el Explorador

- Nueva opción de instalación para mostrar el contenido de SVG en lugar del icono del programa.
- Compatible con SVG normales, declaraciones XML antiguas y vectores Android guardados como `.svg`.
- Registro por usuario, sin reemplazar proveedores existentes; se retira el registro propio al desinstalar.
- Procesamiento mediante el aislamiento de miniaturas de Windows, con límites de recursos y tiempo.
- Pruebas del componente y de la API real del Explorador. Los archivos inválidos o con recursos externos conservan el icono de archivo.

No incluye panel de vista previa ni miniaturas para otros formatos. Si ves iconos en caché, actualiza la carpeta con F5.

## Funciones del visor

Visor de imágenes Lorea para Windows x64, con código propio bajo GPL-3.0-only.

- Vista sobre el escritorio oscurecido y modo ventana.
- Zoom suave con rueda o teclado, desplazamiento y memoria de vista por foto durante la sesión.
- Miniaturas, controles que se ocultan automáticamente y presentación con desvanecido.
- GIF animados y formatos adicionales mediante ImageMagick: WebP, HEIC/HEIF, AVIF, SVG, RAW de cámaras y más.
- Precarga de fotos vecinas, caché limitada y carga de detalle al ampliar.
- Instalación para el usuario actual, registro en «Abrir con» y desinstalación desde Windows.

## Descargar y usar

Descarga `Lorea-Setup.exe`. El instalador incluye las dependencias, las licencias y una copia del código propio correspondiente. No necesitas descargar los códecs por separado.

Windows puede mostrar un aviso porque el instalador aún no está firmado digitalmente. Para establecerlo como visor predeterminado, elige Lorea en «Abrir con» o en Aplicaciones predeterminadas de Windows.

`Lorea-source.zip` contiene el código propio y scripts de compilación. `SHA256SUMS.txt` permite verificar ambas descargas.

## Límites conocidos

Proyecto en desarrollo. No se han probado todas las cámaras ni variantes de archivo. Las animaciones distintas de GIF se muestran como imagen fija. La carga de detalle se limita a 24 megapíxeles por bitmap. El programa no edita las fotos originales y no admite PDF ni videos.

Puedes modificar el programa y compartir tu versión, pero no presentarla como una versión oficial de Lorea. Consulta `BRANDING.md`. Las dependencias conservan sus licencias; consulta `THIRD_PARTY_NOTICES.md`.
