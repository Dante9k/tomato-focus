# 发布与部署台账

本文件记录已经对外提供的版本和发布事实；功能变化仍记录在 [`CHANGELOG.md`](../CHANGELOG.md)，测试范围记录在 [`docs/VALIDATION.md`](VALIDATION.md)。时间统一使用中国标准时间（UTC+8）。最后核对：2026-09-24。

## 记录规则

- Windows 正式版使用 `v<版本>` 标签；macOS 预览版使用 `macos-v<版本>` 标签。已发布标签和附件不覆盖、不重写。
- 每次公开分发记录版本、渠道、源提交、工作流、下载文件和 SHA-256。后续自动发布会额外附带 `release-evidence.json`。
- GitHub Release、GitHub Actions 和 Pages deployment 是机器侧权威记录；本台账用于汇总其关系和发布结论。
- 发布失败只记录在 Actions 历史中，不标记为已发布。回退通过新提交或新版本完成，不删除旧 Release。

## 当前分发状态

| 平台 | 当前渠道 | 版本 | 状态 | 入口 |
| --- | --- | --- | --- | --- |
| Windows x64 | 官网下载 | 1.1.15 | 对外分发；尚无对应 GitHub Release 标签 | [官网](https://dante9k.github.io/tommi/) |
| Windows x64 | GitHub 正式版 | 1.1.14 | 已发布 | [v1.1.14](https://github.com/Dante9k/tommi/releases/tag/v1.1.14) |
| macOS Universal | GitHub 预览版 | 1.1.16 | 已发布；Apple Silicon 与 Intel | [macos-v1.1.16](https://github.com/Dante9k/tommi/releases/tag/macos-v1.1.16) |

## 发布记录

### Windows 1.1.15 · 官网分发

- 发布于：2026-09-24 15:14；源提交：[`52c66e1`](https://github.com/Dante9k/tommi/commit/52c66e1a8e573bf62e2860c651816eaa07a695e9)。
- 结论：Windows CI、macOS CI 和 Pages 部署通过；官网当前返回 1.1.15。
- 证据：[Windows CI](https://github.com/Dante9k/tommi/actions/runs/35968490408)、[Pages 部署](https://github.com/Dante9k/tommi/actions/runs/35968490411)。
- 制品：`TomatoFocus-1.1.15-Setup.exe`，SHA-256 `037B38C83D967C01604B815B847C67D175AF79638401F8D595A0450852D3E2C7`。
- 制品：`TomatoFocus-1.1.15-win-x64.zip`，SHA-256 `AE79E4827C976327D23D87AB9CB01D301B3A49EC4D8686A2C88E382BDA08972E`。
- 备注：`VERSION` 与官网已升级，但尚未创建 `v1.1.15` 标签和 GitHub Release；这项差异保留在台账中，不能将其误记为 GitHub 正式版。

### macOS 1.1.16 · Preview

- 发布于：2026-09-24 14:57；标签：`macos-v1.1.16`；源提交：[`3e499bf`](https://github.com/Dante9k/tommi/commit/3e499bf921f8434821d75e877f678a19135cd841)。
- 结论：GitHub prerelease；macOS 13+，Apple Silicon 与 Intel 通用包；macOS 双运行器验证通过。
- 证据：[Release](https://github.com/Dante9k/tommi/releases/tag/macos-v1.1.16)、[macOS CI](https://github.com/Dante9k/tommi/actions/runs/35967019938)。
- 制品：DMG SHA-256 `CBA1EF053DE4600DD5386BF3241FB7A3A8BFFA2EE32C3E043BF3720ABFECC0F1`；ZIP SHA-256 `B23CDC78EF260ADA9EAA9B5837C75B960E9C05DB5AAC61C3E9B41A0BF733A536`。

### macOS 1.1.15 · Preview

- 发布于：2026-09-24 14:38；标签：`macos-v1.1.15`；源提交：[`8e69771`](https://github.com/Dante9k/tommi/commit/8e697714598e1fa2d714d067ea2defc6155d0c5a)。
- 结论：GitHub prerelease；首个 macOS 通用预览包；已由 1.1.16 替代，历史附件保留。
- 证据：[Release](https://github.com/Dante9k/tommi/releases/tag/macos-v1.1.15)、[macOS CI](https://github.com/Dante9k/tommi/actions/runs/35965386464)。
- 制品：DMG SHA-256 `74F130FBD1C48AA06833D6AAA1D6C77710255775B78FD1734106E002502853B2`；ZIP SHA-256 `EA33C2531B57575F9B6FEECC83D5A567B10CA7E5B872E055D26CF52888F57D70`。

### Windows 1.1.14 · Stable

- 发布于：2026-09-24 09:49；标签：`v1.1.14`；源提交：[`de499bf`](https://github.com/Dante9k/tommi/commit/de499bfb0855f7463977d10dccfa2d809f7d45ea)。
- 结论：首个由标签自动建立的 Windows GitHub 正式版；Release 与 Pages HTTPS 下载均验证通过。
- 证据：[Release](https://github.com/Dante9k/tommi/releases/tag/v1.1.14)、[Release 工作流](https://github.com/Dante9k/tommi/actions/runs/35944550247)、[Pages 工作流](https://github.com/Dante9k/tommi/actions/runs/35943373550)。
- 制品：安装器 SHA-256 `FC9046B9754445D7E0F7345E0C8BDE7B5C284DE17A9303A5AF441F205ECE7BF0`；便携包 SHA-256 `1B77F37AD2C19117284E65C388A32D214CCBA45907B1024DC567771BD8B2F6B6`；官网离线包 SHA-256 `EDE1A91F426F0554D99641FD432914E65DBC33A5B98EC0B4861116252922A76D`。

## 历史开发版本

下列 Windows 版本均于 2026-09-22 完成开发、打包或验证，但在 GitHub 正式发布流程建立前没有独立的公开 Tag/Release。具体变化和验证证据分别保存在 [`CHANGELOG.md`](../CHANGELOG.md) 与 [`docs/VALIDATION.md`](VALIDATION.md)。

| 版本 | 记录状态 |
| --- | --- |
| 1.1.13 | 历史构建；无独立 GitHub Release |
| 1.1.12 | 历史构建；无独立 GitHub Release |
| 1.1.11 | 历史构建；无独立 GitHub Release |
| 1.1.10 | 历史构建；无独立 GitHub Release |
| 1.1.9 | 历史构建；无独立 GitHub Release |
| 1.1.8 | 历史构建；无独立 GitHub Release |
| 1.1.7 | 历史构建；无独立 GitHub Release |
| 1.1.6 | 历史构建；无独立 GitHub Release |
| 1.1.5 | 历史图形安装包；无独立 GitHub Release |
| 1.1.4 | 历史构建及内部网站验收；无独立 GitHub Release |
| 1.1.3 | 历史构建；无独立 GitHub Release |
| 1.1.2 | 历史构建；无独立 GitHub Release |
| 1.1.1 | 历史构建；无独立 GitHub Release |
| 1.1.0 | 历史构建；无独立 GitHub Release |
| 1.0.1 | 历史规范化构建；无独立 GitHub Release |
| 1.0.0 | 初始历史版本；无独立 GitHub Release |
