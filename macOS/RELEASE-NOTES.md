## Tommi for macOS 1.1.18 Preview

Fixes the large tomato jumping between old and new positions when you drag it to dismiss a finished timer. Dragging now moves the resize animation's anchor as well, so subsequent animation frames keep following your movement. The transparent widget uses a backing layer that redraws during resizing.

Regression checks exercise repeated direction changes during dismissal, position samples between drag updates, projectile cleanup and the saved final position. Desktop ghosting on your particular Mac/display still needs confirmation after updating.

Quit Tommi from the menu bar, then replace **Tommi.app** in Applications. Settings and active deadlines are preserved.

To upgrade from 1.1.16 or earlier, quit the old app, remove **Tomato Focus.app** from Applications, then drag **Tommi.app** there. The existing settings directory and bundle identifier remain unchanged, preserving preferences and active deadlines. If Login Items still lists the old copy, remove that entry and enable Launch at login in Tommi if desired.

macOS 13+, universal Apple Silicon and Intel. This is still an ad-hoc signed preview, without Developer ID signing or notarization. [Installation guide](https://github.com/Dante9k/tommi/blob/main/macOS/README.md).

## 简体中文

修复计时结束后拖动大番茄时，缩小动画反复将窗口拉回旧位置的问题。拖动与缩放共用随鼠标移动的位置基准，透明画面改用在调整大小时重绘的图层。

增加持续反向拖动、两次拖动之间动画帧的位置稳定、停止投掷及最终位置保存验证。具体 Mac 和显示器上的桌面残影效果仍需升级后确认。

先从菜单栏正常退出 Tommi，再替换“应用程序”中的 **Tommi.app**。原设置与计时保持兼容。

从 1.1.16 或更早版本升级时，先退出旧版，将“应用程序”中的 **Tomato Focus.app** 移除，再拖入 **Tommi.app**。原设置目录和应用标识保持兼容，保留偏好与正在进行的计时。如果系统登录项仍显示旧程序，移除旧项后，按需要在 Tommi 中重新开启“登录时启动”。

支持 macOS 13 及以上、Apple Silicon 和 Intel。当前仍为临时签名预览版，尚未完成苹果签名和公证。[安装说明](https://github.com/Dante9k/tommi/blob/main/macOS/README.zh-CN.md)。
