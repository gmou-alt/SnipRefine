# SnipRefine · 截改 AI

[简体中文](README.md) | [English](README.en.md) | [Español](README.es.md)

Versión 1.22.1 · Windows · [MIT](LICENSE)

Herramienta de captura de pantalla y edición de imágenes con IA. Permite seleccionar una región, borrar texto, reparar fondos, reemplazar texto y revisar el resultado antes de guardarlo.

## Inicio rápido

Descarga `SnipRefine-1.22.1-win.zip` desde Releases, descomprime y ejecuta `SnipRefine.exe`. Si todavía no hay una publicación, compila el código con `scripts\build.cmd`. Requiere Windows y .NET Framework 4.5 o posterior.

- Ctrl+Alt+A: captura una región y abre el editor.
- Ctrl+Alt+S: captura y guarda una región rápidamente.
- PrintScreen: captura el escritorio completo.
- Ctrl+Z / Ctrl+Y: deshacer / rehacer. F1: ayuda sin conexión.

## Idioma y uso

En la ventana principal, abre **Idioma**, elige 简体中文, English o Español y guarda esa página. Reinicia para aplicar el idioma. Cada página guarda únicamente sus propios ajustes. Algunas descripciones y errores siguen en chino; las traducciones españolas ausentes usan inglés y después el texto original.

El editor contiene herramientas a la izquierda, el lienzo central y el panel de IA a la derecha. El mismo botón permite cerrar y abrir ese panel. Puedes cambiar el tamaño de la ventana y del panel. El borrador restaura los píxeles debajo de anotaciones manuales; no elimina cambios ya integrados en una imagen reabierta.

## Edición con IA y privacidad

Selecciona una región, protege las zonas importantes, describe el cambio y solicita una vista previa. Revisa el resultado antes de aplicarlo y guardarlo. Configura por separado el servicio de edición de imágenes: una conexión de chat no garantiza generación de imágenes. Se admiten servicios compatibles con OpenAI Images Edits y, experimentalmente, Gemini nativo. Consulta [compatibilidad](docs/SERVICE-COMPATIBILITY.md) y [edición de imágenes](docs/IMAGE-EDITING.md) (documentación principalmente en chino).

La edición manual es local. Las solicitudes de IA envían imagen e instrucciones al proveedor configurado y pueden tener coste. Las claves se cifran mediante DPAPI para el usuario actual de Windows. No publiques ajustes, claves ni imágenes privadas. La protección limita los cambios aplicados localmente; no impide que el contexto incluido se envíe al proveedor.

## Desarrollo y verificación

Compila con `scripts\build.cmd`; crea el paquete con `scripts\pack.cmd`. Ejecuta las pruebas de Windows con `tests\run.cmd` y `tests\stress.cmd`.

Esta versión pasó 79 grupos de regresión en Linux/Mono/Xvfb, incluidos servicios HTTP simulados. Esto no sustituye pruebas manuales en Windows. Siguen pendientes la interacción completa, varios monitores con DPI mixto y la calidad de generación real. El ejecutable no está firmado. Un usuario confirmó conectividad real de IA.

El código está en `src/`, las pruebas en `tests/`, los comandos en `scripts/` y la documentación en `docs/`. Para contribuir, consulta [CONTRIBUTING](CONTRIBUTING.md). Usa Issues para informar problemas, sin adjuntar secretos.

## Licencia

[MIT License](LICENSE).
