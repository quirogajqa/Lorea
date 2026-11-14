# Contribuir a Lorea

Gracias por ayudar a mejorar el visor. Mantén la interfaz pequeña y centrada en ver imágenes.

Para reportar un fallo, indica versión de Lorea y Windows, formato de imagen, pasos para reproducirlo y resultado esperado. Comparte una muestra solo si tienes permiso y no contiene datos personales. Los registros del sistema pueden contener rutas y nombres de usuario: revísalos antes de publicarlos.

Para cambios de código, compila con `build.ps1` y verifica lo afectado. En cambios de carga o navegación comprueba apertura por argumento, siguiente/anterior, archivo inválido, miniaturas, zoom, presentación y cierre. Las tareas en segundo plano no deben tocar objetos WPF sin volver al Dispatcher. No mantengas bloqueados los archivos originales.

Las contribuciones al código propio se distribuyen bajo GPL-3.0-only. Los componentes de terceros deben conservar su atribución y licencia. Las versiones modificadas deben identificarse como tales y no presentarse como oficiales; consulta `BRANDING.md`.
