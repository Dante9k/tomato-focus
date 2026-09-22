# 朱果 · Tomato Focus

**一颗番茄，一段完整的专注。**

适用于 Windows 的透明桌面番茄钟。圆润软陶风格、细腻柔光与紧凑尺寸，中央时间滚轮支持惯性滑动；时间到后，番茄从桌面右上角持续抛出，用轻巧的动画提醒你休息。

![朱果番茄钟](docs/images/preview.png)

## 使用

1. 调整番茄中央的 **小时／分钟／秒**。支持滚轮、上下拖动、点击相邻数字；Tab 切换列，方向键微调，也可直接输入数字。
2. **双击绿蒂开始**，番茄随后隐藏。果身底部双击或 Enter 也可开始。
3. 时间到后，番茄出现在原显示器右上角，向整个桌面连续投掷小番茄。
4. **拖动或双击大番茄停止提醒**，也可以按 Esc。

任务栏通知区域的番茄图标可找回窗口、查看剩余时间、取消计时和退出。`···` 菜单提供 25／5／15 分钟预设、提示音开关和 8 秒动画预览。

主窗口约 **317 × 328 个逻辑像素**。可设置 1 秒至 23:59:59；滚轮循环吸附到完整数字。提示音在到期时播放一次，正式动画持续到手动停止。

## 运行条件

- Windows 10 / 11，x64，.NET Framework 4.8。
- 解压便携包，双击 `Tomato.exe`，无需安装或管理员权限。
- 不需要联网，不包含登录、遥测或开机自启。

软件正在迭代中，当前便携包未做企业代码签名。实际性能、混合 DPI、多屏与辅助技术验收边界见 [验证记录](docs/VALIDATION.md)。

## 构建

仓库不依赖第三方 NuGet 包。Windows 自带的 .NET Framework 编译器即可构建：

```powershell
./build.ps1 -Test
./package.ps1
./scripts/check-package.ps1
```

输出位置：

| 输出 | 位置 |
| --- | --- |
| 正式桌面程序 | `build/Tomato.exe` |
| 开发用验证工具 | `build/Tomato.Verify.exe` |
| 可分发程序与文档 | `dist/TomatoFocus-1.0.1-win-x64/` |
| 便携 ZIP 与 SHA-256 | `dist/` |

正式压缩包通过明确的文件清单打包，不包含验证工具、运行状态、日志、调试符号或开发缓存。源代码仓库不跟踪 `build/` 和 `dist/`。

也可以在安装 .NET Framework 4.8 开发工具的 Visual Studio 中打开 **`Tomato.Focus.sln`**，选择 x64 构建。

## 验证

```powershell
# 无窗口逻辑验证
./build/Tomato.Verify.exe --self-test

# 渲染真实控件，检查编辑态和提醒态图片
./build/Tomato.Verify.exe --render-preview

# 需要交互桌面，会显示约 17 秒动画，结束后自动退出
./build/Tomato.Verify.exe --smoke-test
```

结果位于 `build/*-results.txt`。桌面流程验证使用独立状态文件，不操作日常计时状态。自动流程调用应用动作，不替代实际鼠标和触屏验收。

GitHub Actions 在 Windows 上执行构建、逻辑验证、控件渲染、打包及校验，并保存短期构建产物。桌面动画验收需在可交互 Windows 会话中执行。

## 项目结构

```text
Tomato.Focus.sln
src/Tomato.Focus/
  Application/       生命周期、托盘、计时协调
  Domain/            倒计时状态机与运动物理
  Infrastructure/    状态存储、偏好设置、Windows 互操作
  Presentation/      窗口、滚轮、动画和素材绘制
  Properties/        程序元数据
tests/Tomato.Focus.Verification/
scripts/             构建、打包、发布包检查
assets/              内嵌图像、图标与素材来源
docs/                架构、验收记录和预览图
.github/             自动构建、Issue 和 PR 模板
```

## 状态与隐私

计时按照 UTC 截止时刻判断，休眠时间计入倒计时；程序不会主动唤醒电脑。重新打开程序时会恢复未到期计时或显示已到期提醒，程序关闭期间不会后台触发。

状态保存在当前用户的 `LocalApplicationData/TomatoFocus/state.xml`，通过临时文件和原子替换写入；损坏时使用默认设置。错误日志只留在本机。透明动画层允许点击穿透，停止提醒后解除快捷键并回收动画资源。

## 开发文档

- [架构说明](docs/ARCHITECTURE.md)
- [验证记录](docs/VALIDATION.md)
- [贡献指南](CONTRIBUTING.md)
- [安全说明](SECURITY.md)
- [版本变更](CHANGELOG.md)
- [素材来源与提示词](assets/ARTWORK.md)

本项目目前**保留所有权利**，未默认授予开源许可，详见 [LICENSE](LICENSE)。
