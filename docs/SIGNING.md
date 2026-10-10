# Windows 数字签名

当前交付的 1.22.1 EXE **未数字签名**。版本资源和 DPI 清单不等于签名。

维护者需要有效代码签名证书及其私钥、Windows SDK SignTool 和证书机构提供的可信时间戳 URL。证书应在当前用户证书存储中；不要把 PFX、私钥或密码放进项目。

在 Windows 命令提示符设置 `SNIPREFINE_SIGN_CERT_THUMBPRINT` 和 `SNIPREFINE_TIMESTAMP_SERVER` 后运行 `scripts\pack.cmd`，脚本会构建、签名、验证再打包。环境变量未设置时生成未签名包；签名或校验失败时中止。

也可从 PowerShell 在构建后执行：

```powershell
.\scripts\sign.ps1 -CertificateThumbprint <证书指纹> -TimestampServer <证书机构时间戳URL>
```

脚本使用 SHA-256，检查签名发布者、有效状态和时间戳。手工签名后再次运行普通构建会覆盖签名，请通过带上述变量的打包流程发布。脚本尚未使用真实证书实机执行。

签名不能保证 SmartScreen 不提示；其行为还取决于信誉和设备策略。不得在未签名包中宣称“已签名”或“无安全提示”。
