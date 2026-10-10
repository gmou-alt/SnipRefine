# 项目文件与开发入口

源码仓库包含 Windows 截图与 AI 辅助编辑软件的源码、图标、文档和构建发布工具；可运行程序通过 Releases 单独分发。

## 目录内容

| 目录 | 内容与作用 |
|---|---|
| `src/` | C# 应用源码，包括截图框选、图片编辑、AI 对话、设置页面和界面控件。 |
| `assets/` | 软件图标：`app.ico` 用于 Windows 程序，`sniprefine-icon.png` 为图标原图。 |
| `scripts/` | Windows 编译、启动、安装和打包脚本。 |
| `tools/` | 图标制作和版本号更新工具。 |
| `tests/` | 功能回归、请求取消、截图模式、侧边栏动画及资源压力检查。 |
| `docs/` | 当前使用说明、图片编辑说明、发行说明和 Windows 验证说明。 |
| `internal-skills/image-edit/` | AI 图片编辑能力说明，描述支持的操作及执行规则。 |
| `.github/workflows/` | GitHub Actions 自动构建与程序打包流程。 |
| `.github/ISSUE_TEMPLATE/` | 问题反馈和功能建议模板。 |
| `dist/` | 编译后的程序和程序发行压缩包，由构建、打包脚本生成。 |

## 根目录文件

| 文件 | 作用 |
|---|---|
| `README.md` | GitHub 首页：产品介绍、快速开始与开发入口。 |
| `README.en.md`、`README.es.md` | 英文与西班牙文首页（功能与验证范围同步说明）。 |
| `CHANGELOG.md` | 当前发行内容说明。 |
| `LICENSE` | MIT 许可证。 |
| `CONTRIBUTING.md` | 开发约定及贡献说明。 |
| `.gitignore` | Git 文件忽略规则。 |
| `.gitattributes` | Git 文本及换行处理规则。 |
| `.editorconfig` | 编辑器编码、缩进及换行约定。 |

## 主要源码

| 文件或模块 | 作用 |
|---|---|
| `Program.cs` | 程序启动、版本与初始化。 |
| `TrayApp.cs` | 托盘菜单、截图调度、剪贴板及后台保存。 |
| `MainForm.cs`、`MainAiSettings.cs` | 主窗口、设置页面和 AI 服务商配置界面。 |
| `Config.cs` | 软件配置的读取与保存。 |
| `CaptureHotkeys.cs`、`HotkeyWindow.cs` | 快捷键注册及消息接收。 |
| `OverlayForm.cs`、`SelectionGeometry.cs` | 截图覆盖层和选区移动、缩放计算。 |
| `EditorForm.cs`、`EditorSelection.cs` | 截图编辑、标注选择、文字输入及结果输出。 |
| `Shape.cs`、`UndoStep.cs` | 标注数据与撤销记录。 |
| `AiClient.cs`、`AiProvider.cs`、`AiPreset.cs` | AI 请求、提供商配置及模型预设。 |
| `AiProviderDiscovery.cs`、`AiProviderRow.cs` | 模型列表获取和提供商卡片。 |
| `AiChat.cs`、`ChatPanel.cs`、`ChatMessageView.cs` | 对话历史、发送交互与左右聊天气泡。 |
| `ChatAttachments.cs` | 对话附件选择、缩略图及图片修改入口。 |
| `EditorAiSession.cs`、`EditorAiReview.cs`、`EditorAiProgress.cs` | 图片编辑请求、修改确认、前后对比与请求进度。 |
| `EditorAiRegion.cs`、`AiRegionRequest.cs` | AI 局部选区、局部 PNG 编码与坐标映射校验。 |
| `ImageEditClient.cs`、`ImageEditGemini.cs`、`ImageEditPatch.cs`、`ImageEditSettings.cs` | 图片编辑服务请求（Images Edits 与 Gemini 原生）、蒙版合成与配置。 |
| `EditorImageEdit.cs`、`MainImageEntry.cs` | 图片编辑任务与预览、打开本地图片入口。 |
| `AiOp.cs`、`AiReply.cs`、`AiImageEditSkill.cs` | AI 编辑指令、回复数据及内部编辑规则。 |
| `AiSetupWizard.cs`、`AiTroubleshooting.cs` | 快速配置、连接和看图验证、错误处理建议。 |
| `EditorSave.cs`、`EditorTextResult.cs` | 独立保存、另存为、失败恢复与文字结果窗口。 |
| `SavedHistory.cs` | 可选跨重启历史索引，仅保存本机路径与时间。 |
| `AiRequestControl.cs` | 请求取消与超时控制。 |
| `RecentShots.cs`、`MainRecentShots.cs` | 最近截图缓存与展示、可选保存历史。 |
| `ImagePreviewForm.cs` | 图片预览、缩放和拖动。 |
| `UserGuide.cs`、`UserGuideData.cs` | 离线帮助窗口与说明内容。 |
| `UiText.cs`、`UiTextLanguages.cs`、`UiHelpData.cs` | 界面多语言资源（简体中文、English、Español 回退规则）与英文帮助内容。 |
| `Dsh*.cs`、`GlassSurface.cs`、`ChatGlassSurface.cs` | 共享主题、玻璃效果及自绘控件。 |
| `SidebarMotion.cs`、`SidebarTransition.cs` | 侧边栏动画与边缘过渡。 |
| `WindowChrome.cs`、`ThemedDialog.cs` | 窗口边框、缩放、全屏和软件弹窗。 |
| `Native.cs` | Windows 系统接口封装。 |
| `PrivacyGuard.cs` | AI 接口地址校验。 |
| `Diag.cs` | 诊断接口及清理辅助。 |
| `ToastForm.cs` | 操作提示窗口。 |
| `FloatingIcon.cs` | 悬浮图标控件代码。 |

## 脚本与工具

| 文件 | 作用 |
|---|---|
| `scripts/build.cmd` | 编译 `src/` 源码，生成 `dist/SnipRefine.exe`。 |
| `scripts/pack.cmd` | 编译并生成程序发行 ZIP。 |
| `scripts/install.cmd` | 创建启动入口并配置安装相关设置。 |
| `scripts/SnipRefine.ps1` | 通过 PowerShell 在内存中编译并启动源码。 |
| `scripts/SnipRefine.vbs` | 隐藏控制台启动 PowerShell 启动器。 |
| `scripts/sign.ps1` | 可选的代码签名与校验脚本（需自备证书）。 |
| `scripts/bump-version.cmd` | 调用版本更新工具。 |
| `tools/set-version.ps1` | 同步程序和打包脚本版本号。 |
| `tools/make-icon.ps1` | 制作 Windows 图标资源。 |
| `tests/run.cmd` | 构建并运行 Windows 回归检查。 |
| `tests/stress.cmd` | 运行资源压力检查。 |

## 文档与程序文件

| 文件 | 作用 |
|---|---|
| [USER-GUIDE.md](USER-GUIDE.md) | 截图、编辑、AI、附件、设置与快捷键使用说明。 |
| [IMAGE-EDITING.md](IMAGE-EDITING.md) | AI 图片编辑服务的配置、流程与限制。 |
| [AI-REGION.md](AI-REGION.md) | AI 局部选区：请求范围、坐标映射与边界保护。 |
| [SERVICE-COMPATIBILITY.md](SERVICE-COMPATIBILITY.md) | 支持的图片编辑服务格式与能力范围。 |
| [RELEASE-NOTES.md](RELEASE-NOTES.md) | 当前发行版功能说明。 |
| [WINDOWS-CHECKS.md](WINDOWS-CHECKS.md) | Windows 检查方式和验证范围。 |
| [ACCEPTANCE.md](ACCEPTANCE.md) | 人工验收记录模板。 |
| [AI-EXPERIENCE-CHECKS.md](AI-EXPERIENCE-CHECKS.md) | 合成样图与真实图片编辑服务的验收步骤。 |
| [PROGRESS-REVIEW.md](PROGRESS-REVIEW.md) | 当前进度、已执行验证与后续优先级。 |
| [SIGNING.md](SIGNING.md) | 数字签名状态与签名流程说明。 |
| docs/fixtures/*.png | 图片编辑验收用合成样图。 |
| `internal-skills/image-edit/SKILL.md` | AI 图片编辑能力说明。 |
| `dist/SnipRefine.exe` | 可执行程序。 |
| `dist/SnipRefine-1.22.1-win.zip` | 供最终用户下载的程序发行包。 |

完整源码包用于开发与构建；`dist/` 下的程序发行包用于运行软件。

AI 局部编辑由 `src/EditorAiRegion.cs` 提供框选与目标预览，`src/AiRegionRequest.cs` 处理 PNG 和坐标验证，`src/EditorAiSession.cs` 发送局部请求。`tests/AiRegionTests.cs` 覆盖选区上传、坐标、边界与历史。

图片生成编辑：`ImageEditClient.cs` 请求/响应，`ImageEditPatch.cs` 蒙版和合成，`ImageEditSettings.cs` 配置，`EditorImageEdit.cs` 任务及预览，`MainImageEntry.cs` 打开本地图片。验证位于 `ImageEditTests.cs` 和 `ImageEditTransportTests.cs`。
