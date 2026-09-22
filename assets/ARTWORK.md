# 番茄素材

当前运行素材为 `tomato-cute.png`，应用和安装器图标统一使用 `brand/tomato-focus.ico`。旧的 `tomato-cute.ico` 已移除；以下各版本说明保留素材演变记录。

## 1.1.13：圆角镂空数字

`RecessedGlyph` 的0–9和冒号为本项目原创矢量路径，使用统一的圆端点和曲线笔画，非第三方字体文件。内置字形用于专注读数，淡白内透光、内壁遮挡和下沿反光由同一矢量渲染器绘制。

## 1.1.10：柔软碰撞音效

落地声改用Kenney的CC0拟音素材，经离线剪裁、滤波和响度调整后嵌入程序；素材原件、许可及处理记录见 [音效来源](audio/README.md)。投出声为短促原创带限空气声。以下1.1.3的纯合成撞击音记录仅用于历史追溯，不再用于当前版本。

## 1.1.5：品牌 Logo 与安装包图标

新增 `brand/tomato-focus.png` 品牌母版及多尺寸 ICO，用于安装包、应用程序文件、快捷方式、安装窗口及网站 favicon。使用内置 imagegen 生成，来源与完整提示词见 [品牌图标说明](brand/README.md)。桌面番茄与投掷动画继续使用现有 `tomato-cute.png`。

## 1.1.3：投掷与碰撞拟音

`ThrowFeedback.CreateSound` 原创程序合成六组单声道浮点样本，启动时预生成：三组150–174ms掠空声、三组220–244ms软果实撞击声。滤波噪声表现空气和果肉摩擦，快速衰减的低频音体表现重量；首尾渐变并去除明显直流偏移。属于拟真声音设计，并非实物录音或外部素材。

播放时根据物理事件调节音量和立体声声像，最多16声部混合并软限幅。开发预览 `build/throw-impacts.wav` 使用与应用相同的飞行事件、样本和混音器生成，可用于试听连续投出、落地和轻回落。

## 1.1.2：拨轮音效

`WheelFeedback.CreateClick` 原创合成三种 40ms、44.1kHz、16bit 单声道 PCM 卡点声。短促噪声接触、快速衰减的低频音体与 6ms 后的轻微落齿声叠加；音色及响度变化很小，预加载后交替播放。没有使用或提取 Apple 的录音。开发预览工具输出 `build/wheel-detents.wav`，便于慢速与快速连续拨动试听。

## 1.1.0：更小巧的无表情番茄

当前 `tomato-cute.png` 使用内置 image_gen 编辑前一版素材，保留原始透明 alpha；未使用 CLI/API 回退。程序图标由同一素材的 WPF 渲染结果导出为 `tomato-cute.ico`。原版可从 Git 历史追溯。拨轮声为 `WheelFeedback.CreateClick` 原创合成的 24ms PCM，无 Apple 音频素材。

完整提示词：

> Use case: precise-object-edit. Edit target: supplied tomato sprite for a premium cute Windows pomodoro desktop widget. Make it more refined and softly cute with a plump rounded slightly squat silhouette, shorter adorable green stem and fewer rounded thick leaf lobes, vivid fresh green. Coral tomato red with restrained soft jelly/clay satin highlights and subtle fine texture; premium 3D rendered tactile material, not plastic hard shine. Front facing, balanced symmetric shape, big calm front body to overlay timer controls. No face, eyes, mouth, text, numerals, UI, extra objects, floor or cast floor shadow. Center single complete tomato on genuinely transparent background, square 1024 composition, nearly fills frame with 4% clear margin, stem fully visible. Preserve red tomato identity and excellent clean alpha edges.

## 1.0.1：圆润软陶风格

`tomato-cute.png` 使用内置 image_gen 编辑工具，以原始 `tomato.png` 为参考制作。保留透明 alpha，采用圆润果身、厚实绿叶、珊瑚红与柔和高光。桌面窗口、投掷粒子、托盘和程序图标共同使用新版素材；原版从 Git 历史追溯，不再保留于当前工程目录。`tomato-cute.ico` 从实际软件渲染图导出。未使用 CLI/API 回退。

新版完整提示词：

> Use case: style-transfer. Edit target: attached transparent tomato sprite used in an existing desktop pomodoro app. Change the art style to noticeably cuter, premium stylized 3D clay / soft vinyl toy illustration. Make it plump and rounded with softened broad lobes, smooth tactile satin skin, lively coral vermilion red, gentle warm highlights and subtle fine material grain rather than realistic pores. Turn the leafy crown into five rounded chunky green leaves, fresh pistachio and jade tones, and a short charming curved stem. Sophisticated adorable collectible design, warm and friendly without looking cheap. Preserve the original overall framing, scale, bounding box and frontal view: a single tomato with stem above, body takes up same area, broad unobstructed front so countdown controls will be added by software. NO face, eyes, mouth, limbs, text, UI or numbers (the face area is reserved for the interactive timer). Preserve genuinely transparent alpha background. No backdrop, no floor, no cast shadow, no outline, no watermark. High quality soft studio lighting, beautifully modelled volume, avoid photorealism and harsh specular reflections.

## 原始版本：写实果皮

历史素材 `tomato.png` 为本项目通过内置 image_gen 工具生成的透明 PNG，1254 × 1254，保留原始 alpha 通道。当时作为程序集资源嵌入，现已由 `tomato-cute.png` 替代；旧 PNG 和 ICO 可从 Git 历史恢复。使用内置工具，未使用 CLI/API 回退。

完整提示词：

> Use case: stylized-concept. Asset type: production transparent PNG sprite for a luxury desktop pomodoro timer, not a UI mockup. Generate ONE ripe red tomato isolated on a genuinely TRANSPARENT alpha background, square image 1024x1024. Exquisite premium 3D product render, physically based materials, rich saturated vermilion red skin with very fine natural pores and tiny golden speckles, soft broad studio highlight on upper left, rich crimson shaded lower right, believable rounded volume, subtle natural lobes. Elegant vivid green five-pointed leafy calyx and short curved upward stem at top. View almost straight on, slightly above enough to see leaves. Composition is important for existing software: tomato fruit body spans x=9% to 92%, y=21% to 87%, broad and rounded heartlike tomato silhouette; calyx centered at x=51%, y=24%, stem tip at x=55%, y=12%. Bottom of fruit around y=87%. The central and lower frontal face must be unobstructed red skin, so software can overlay timer controls. No UI, no letters, no numbers, no clock, no hands, no decorations, no watermark, no pedestal. NO cast shadow or ground plane: transparency surrounds fruit and stem, including below. Crisp antialiased cutout, polished art direction, tactile and beautiful, realistic but slightly idealized icon sculpture.
