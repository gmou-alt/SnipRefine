# SnipRefine

[简体中文](README.md) | [English](README.en.md) | [Español](README.es.md)

Version 1.22.1 · [MIT](LICENSE)

A Windows tool focused on selected-region AI image editing: remove text, repair backgrounds, replace text and preview local image edits.

## Start

Download `SnipRefine-1.22.1-win.zip` from the repository Releases page, extract it, and run `SnipRefine.exe`. If no release is published, build source with `scripts\build.cmd`. Windows and .NET Framework 4.5+ are required; no modern .NET SDK or NuGet packages are needed.

Ctrl+Alt+A captures an adjustable selection; Ctrl+Alt+S quickly saves a selection; PrintScreen captures the combined desktop. Use drawing tools, Ctrl+Z/Ctrl+Y, Save or Copy. F1 opens offline help.

Settings > Language offers Simplified Chinese, English and Español. Save the page and restart to apply. Core editing controls are localized; Spanish falls back to English, then source text. Some descriptions and errors remain Chinese.

## Regional editing

Click AI region in the editor, drag a rectangle and enter a request. Only the local PNG and current instruction are uploaded; previous conversation is omitted. Review numbered targets and recognized text before applying. Local operation rendering is clipped to the approved region; undo/redo retains clipping. Clear AI region returns to whole-image mode. Local crop remains unavailable in the instruction workflow. Separate image-edit tasks support protected masks and previews; see [image editing](docs/IMAGE-EDITING.md). See [regional editing](docs/AI-REGION.md).

## AI and privacy

Configure a provider, API key and model in AI settings. Test text and image input separately. AI edits support annotation, crop, redaction and OCR commands, with review and undo. The ordinary chat-based instruction mode does not generate pixels. The separate image-edit workflow calls a compatible Images Edits service, or the experimental Gemini native protocol; real-service quality remains unverified for both. See [service compatibility](docs/SERVICE-COMPATIBILITY.md).

Manual editing stays local. AI requests send the image and text to your configured endpoint. Latest-image auto-attachment is off by default. Keys use Windows current-user DPAPI encryption; never publish configuration or private images. Inspect AI output before saving.

## Development and verification

Build: `scripts\build.cmd`. Package: `scripts\pack.cmd`. Windows tests: `tests\run.cmd` and `tests\stress.cmd`.

Linux/Mono regression results do not substitute for Windows acceptance. Real AI connectivity has been confirmed by a user; full vision/model coverage and mixed-DPI Windows interaction remain pending. The supplied executable is unsigned. See [verification](docs/WINDOWS-CHECKS.md), [acceptance](docs/ACCEPTANCE.md), [signing](docs/SIGNING.md) and [contribution guide](CONTRIBUTING.md).

## Workspace

Startup opens the original main window with settings available directly. There is no separate home screen or settings window. The editor uses a left tool palette, central canvas, and right AI panel. Save, copy, undo, and zoom are at the top. Each settings page has one independent save button at the bottom.

Version 1.22.1 passed 79 regression groups on Linux/Mono/Xvfb. Windows interaction, multi-display/high-DPI behavior, and real image-generation quality remain unverified.

The eraser restores underlying image pixels beneath manual annotations. It supports partial strokes, transparency, undo and redo. It does not erase the underlying image or annotations already flattened into a reopened image. AI commands are grouped, and the effect brush labels identify Mosaic, Redact and Blur.

Version 1.22.1 is an experience trial: contextual tool properties, common tools first, image-edit task entries first, and persistent image save state. Keep your previous release package if you may need to roll back.

[AI service compatibility / 服务兼容与多语言范围](docs/SERVICE-COMPATIBILITY.md)
