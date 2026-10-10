# 参与开发

保持 C# 5、.NET Framework 4.5 引用兼容及零第三方运行依赖。源码使用 UTF-8 无 BOM、LF。编码约定：`.cmd` 脚本只写 ASCII 注释（批处理按 OEM 代码页解码，中文会导致解析错位）；含中文的 `.ps1` 必须带 UTF-8 BOM，否则 PowerShell 5.1 会按 ANSI 解码而解析失败；换行由 `.gitattributes` 统一（`*.cmd` / `*.ps1` 入库为 CRLF，`.cs` 为 LF）。

执行 `scripts/build.cmd` 编译，执行 `tests/run.cmd` 验证 Windows 回归。请描述问题、修改结果、已运行检查和未验证事项。

不要提交用户配置、API 密钥或缓存图片；`dist/` 等构建产物由脚本生成，不入库。
