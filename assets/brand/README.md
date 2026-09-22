# 朱果品牌图标

`tomato-focus.png` 是品牌母版，`tomato-focus.ico` 是 Windows 多尺寸图标。与桌面番茄绘图使用的 `../tomato-cute.png` 分离：换品牌图标不改变桌面计时器或投掷动画的外观。

用途：应用程序与安装程序文件图标、安装后的快捷方式、安装窗口品牌区和网站 favicon。Windows ICO 包含 16、20、24、32、40、48、64、96、128、256 像素的透明 PNG 帧，兼顾小图标、列表及高 DPI 显示。

这套品牌是所有后续版本的默认配置，不绑定某个版本号。母版和安装窗口源码长期保留在工程中，版本升级无需重新配置 Logo。

运行 `./package.ps1` 会自动从母版导出多尺寸图标，构建应用、便携 ZIP 和带品牌界面的 EXE 安装包，校验安装载荷并生成 SHA-256 文件与安装窗口预览。应用和安装包的文件图标、安装后的快捷方式始终使用同一品牌图标。版本号统一读取仓库 `VERSION`，自动构建也执行相同流程并保存两种安装产物。

`./scripts/build-website.ps1` 复用上述打包流程，再生成网站下载文件。单独更新图标可运行 `./scripts/build-brand-icon.ps1`。安装窗口预览位于 `build/installer-preview.png`，预览模式不安装应用或创建快捷方式。品牌素材缺失会使构建失败，不会回退到默认图标。

## 生成来源

2026-09-22 使用内置 imagegen 生成，并进行一次透明边缘清理。未使用 CLI/API 回退。导出脚本只缩放和封装格式，保留透明通道。素材权利说明见仓库 LICENSE。

首次生成完整提示词：

> Use case: logo-brand. Asset type: a finished Windows application/installer icon for an independent Chinese pomodoro desktop app named 朱果 / Tomato Focus. Create ONE polished, distinctive, professionally designed app icon, no variations, no presentation board. A precise softly rounded square tile in vivid vermilion tomato red (#DF442E family), containing an elegant bold ivory-white abstract tomato combined with a simple timer: the tomato silhouette has a subtle top notch and one sculptural fresh green leaf; a clean negative-space clock hand and small circular center quietly communicate focus/time. Aim for a coherent single geometric mark, exceptionally simple and legible at 16–32 pixels; broad silhouettes, generous separation, no thin strokes, no numerals, no ticks. Premium restrained very shallow dimensionality, precise vector-like edges, tiny soft highlights, contemporary productivity software identity. Centered front view, square 1024 composition; tile fills about 90% of canvas with consistent safe margin, genuinely transparent alpha outside rounded tile. No text, no letters, no trademark symbols, no shield, no download arrow, no badges, no photographic tomato, no cartoon face, no mockup, no device, no background scenery, no checkerboard texture. The complete asset must look finished as an ICO in Windows Explorer on both white and dark backgrounds.

边缘清理完整提示词：

> Use case: precise-object-edit. Edit this finished application icon ONLY to clean its alpha edges. Preserve the exact red rounded-square tile, ivory tomato-clock mark, green leaf, proportions, colors, lighting and current composition. Remove ALL stray detached red pixels, red speckles, noise, fringe and fragments outside the rounded-square tile, particularly above, below, and at the right. Outside the single clean antialiased rounded-square tile must be genuinely transparent with zero stray marks. Maintain a consistent transparent margin. No other visual changes, no new elements, no text. Deliver one square transparent PNG suitable for a professional Windows app icon.
