# 投掷音效素材

落地音效使用 Kenney 的 **Impact Sounds 1.0** 中三个 `impactSoft_medium_000/001/002.ogg` 柔软接触素材。

- 作者：Kenney（https://kenney.nl/）
- 原始页面：https://kenney.nl/assets/impact-sounds
- 下载日期：2026-09-22
- 许可：CC0 1.0，https://creativecommons.org/publicdomain/zero/1.0/
- 作者随包说明保存在 `source/Kenney-License.txt`；三个选定原件保存在 `source/`。

`scripts/prepare-impact-audio.py` 将素材合并为单声道、去除首尾静音、轻微加速、削减低频闷响和高频毛刺，并作短淡入淡出与响度匹配，生成44.1kHz/16位PCM。输出长度约89–117ms，峰值不超过0.38。源文件及输出哈希记录在 `processing.json`。离线重制需要 Python、numpy 和 soundfile 0.13.1；日常构建与正式程序不需要这些依赖或联网。

三个 WAV 嵌入程序集，在启动时一次性解码。投出音为项目原创75–85ms带限空气噪声，未使用固定音高或滑音；落地不再叠加旧版125Hz正弦波。反弹后二次触地的音量系数为0.35。无混响、无长尾、无实时重采样。全部声音仍由实际物理事件触发。

这些素材属于拟音设计，不是本项目自行录制的番茄实物落地声音。波形和设备验证不替代用户对音色的听感判断。
