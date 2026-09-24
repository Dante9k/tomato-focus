# 贡献指南

本项目目前保留所有权利，提交代码前请先与维护者确认贡献安排，参阅 [LICENSE](LICENSE)。

## 开发环境

- Windows 10 / 11 x64 与 .NET Framework 4.8。
- PowerShell 7 推荐；基础构建脚本兼容 Windows PowerShell 5.1。
- Visual Studio 2022 与 .NET Framework 4.8 开发工具可选；打开 `Tomato.Focus.sln`。
- 不需要第三方 NuGet 包或网络连接即可使用脚本构建。

## 工作流程

Mac 版位于 `macOS/`，需要 macOS 13 及以上与 Swift 5.9 / Xcode 命令行工具。在 Mac 运行 `bash scripts/build-macos.sh` 完成领域测试、双架构编译、隔离启动验证、签名校验和 DMG / ZIP 打包；打包检查使用系统 Python 3 标准库，不需要第三方包。领域组件在 `TomatoCore`，窗口、声音与输入在 `TomatoFocus`。不要把 Mac 设置 JSON、Swift 构建缓存或签名资料提交到仓库。真实触控板与显示器验收项目见 [macOS/VALIDATION.md](macOS/VALIDATION.md)。以下流程适用于 Windows 项目。

1. 从默认分支创建独立功能分支，变更限定于一个明确目的。
2. 遵守 `.editorconfig`，使用 PowerShell 7 运行 `./scripts/format-code.ps1` 统一格式。领域逻辑不直接访问窗口、文件系统或系统托盘。
3. 运行 `./build.ps1 -Test`；界面变化再运行 `./build/Tomato.Verify.exe --render-preview` 并检查图片。
4. 改动窗口、输入或生命周期时，在可交互桌面运行 `./build/Tomato.Verify.exe --smoke-test`，再进行相关鼠标、键盘手工验收。
5. 运行 `./package.ps1`，检查压缩包仅包含清单内的文件。
6. 提交 PR，说明具体行为变化、验证证据和限制。提交消息推荐 `feat:`、`fix:`、`refactor:`、`docs:` 或 `chore:`。

不要提交本机状态 XML、调试符号、构建输出、日志、账号资料、令牌或签名证书。截图只截取应用自身，避免桌面私人内容。

网站构建使用 `./scripts/build-website.ps1`，包含正式程序、安装器与网站白名单校验；可独立运行 `./scripts/check-website-package.ps1` 检查网站包。浏览器验证依赖及步骤见 `website/README.md`。音效重制和宣传片制作是可选开发任务，依赖分别列在 `scripts/requirements-audio.txt` 与 `marketing/xiaohongshu/requirements.txt`，建议安装在仓库外的 Python 虚拟环境中；正式应用构建不需要 Python 或 Node.js。

Windows 发布包由 `VERSION` 指定版本；变更版本时同步更新 `AssemblyInfo.cs` 和 `CHANGELOG.md`。Mac 发布包单独使用 `macOS/VERSION`，修改后同步 Mac 说明与版本记录。企业签名证书不得放入仓库，签名应在受控发布环境完成。

仓库首页使用英文 `README.md`，中文对应 `README.zh-CN.md`，两份均随安装包分发。修改使用方法、安装行为或版本号时同步两种语言。安装器检查使用 `Tomato.Verify.exe --installer-test <Setup.exe>`，由打包脚本自动执行；它只写入独立临时目录，不改变真实桌面快捷方式或日常计时状态。
