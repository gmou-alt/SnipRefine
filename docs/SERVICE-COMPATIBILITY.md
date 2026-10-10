# AI 服务兼容性

| 用途 | 请求协议 | 状态 |
|---|---|---|
| 文本、看图、OCR、指令标注 | OpenAI Chat Completions 兼容、Anthropic Messages | 已有适配；模型须支持对应输入 |
| 生成式图片修改 | OpenAI Images Edits multipart | 保留现有适配；须返回 b64_json 和1024方形图 |
| 生成式图片修改 | Gemini 原生 generateContent JSON | 实验适配；本地模拟通过，真实服务待验证 |

## Gemini 配置

打开图片编辑设置，选择 Gemini Native (Experimental)。填写支持图片输出的模型 ID，例如 `gemini-2.5-flash-image`，以及完整地址 `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent`。模型 ID 必须与地址中的模型保持一致；账号模型可用性以服务商控制台为准。密钥单独填写，禁止加到地址查询参数中。

该协议上传原图、文字要求及黑白权限参考图。Gemini 无原生等价蒙版保证；生成内容可能不完全遵守参考图。软件在本机只回写允许修改的像素，严格保留保护区与选区外原图，但允许区域的编辑质量仍需预览确认。保护区和上下文仍会上传。

仅接受内嵌 PNG/JPEG/WebP 图像；实际图片解码能力取决于系统，PNG/JPEG 优先。不接受外部图片 URL。非方形结果拒绝；256–4096方形图规范化到1024后映射并预览。不会自动重试或探测其他协议，避免重复计费。

官方协议参考：[Gemini 图片生成与编辑](https://ai.google.dev/gemini-api/docs/generate-content/image-generation)、[OpenAI 图片编辑](https://developers.openai.com/api/reference/resources/images/methods/edit)。

## 多语言范围

独立的语言页提供简体中文、English、Español，保存本页后重启生效。核心截图工具、AI 分组、上传范围、预览按钮及图片服务设置已扩充资源；尚有未翻译描述和错误提示。西班牙文缺项回退英文，再回退原文。用户文字、OCR 和服务回复不会自动翻译；离线帮助主要为中/英文。
