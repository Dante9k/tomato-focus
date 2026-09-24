# Tomato Focus for macOS

English | [简体中文](README.zh-CN.md)

The native Mac edition of **朱果 · Tomato Focus**, built with Swift, AppKit and SwiftUI. No Electron runtime or third-party packages. Supports **macOS 13 Ventura or later**, with one universal app for **Apple Silicon and Intel**.

## Install

Download the Mac disk image or ZIP from [GitHub Releases](https://github.com/Dante9k/tommi/releases), or the `tomato-focus-macos-universal` artifact from a successful [macOS build](https://github.com/Dante9k/tommi/actions/workflows/macos.yml).

Open the DMG and drag **Tomato Focus.app** into Applications, or copy the app to another writable folder. Open the app; a small tomato appears on your desktop and in the menu bar, without a Dock icon. **Launch at login is enabled by default on the first launch**, including the first launch after upgrading from 1.1.15. You can turn it off in Preferences. macOS may require approval under System Settings → General → Login Items; the app shows the actual registration/approval state and provides a shortcut there. It does not repeatedly re-enable an item you remove in System Settings. Quit an existing copy from its menu-bar menu before replacing it. Settings and active deadlines survive replacement.

This initial Mac edition is **ad-hoc signed, not Developer ID signed or notarized**. macOS may block a downloaded copy. Only if you trust this repository and have checked the download, use Apple's per-app **System Settings → Privacy & Security → Open Anyway** flow. Never disable Gatekeeper or remove quarantine in bulk. See [Apple's guidance](https://support.apple.com/en-us/102445). Organization policies can prevent opening it; there is no automated bypass.

## Use

- Scroll or drag the three time wheels, click adjacent digits, or use Tab, arrow keys and number keys. Hours, minutes and seconds have accessible names without visible unit labels. Wheel sound and optional Force Touch feedback follow real value changes; presets remain silent.
- The idle tomato is now **220 × 220 points**, 12% smaller in each dimension. Its drawing, time wheels and pointer targets scale together; selected digits remain about 21 points tall and each column is about 42 points wide.
- Double-click the stem or lower fruit, or press Return to start. The widget shrinks toward its upper-right corner from 220 to 125 points over 460 ms. The fruit fades to 32% opacity, while rounded white cutout digits remain legible. The editing frame disappears. The floating panel stays above ordinary windows and joins Spaces. The reminder grows to 250 points; stopping it or canceling focus returns to the 220-point editor. Saved positions retain their upper-right anchor across upgrades.
- Drag to move. While holding it, make three deliberate reversals in one second (at least 20 points each, along one main axis) to cancel. Ordinary dragging does not cancel. The menu-bar menu can also cancel, hide or restore the widget.
- When time expires, the tomato grows at its current position and throws small tomatoes across **the display containing the widget**. Sounds follow launches and physical landings. The overlay lets clicks reach other apps. Drag or double-click the large tomato, press Escape when the widget has keyboard focus, or use **Dismiss reminder** in the menu bar to stop.
- Right-click the tomato or choose **Preferences** from its menu-bar menu. Choose 25/5/15 minutes, configure wheel/completion/effect sounds independently, toggle trackpad feedback, or preview the reminder for eight seconds. The interface follows the system's English/Chinese language preference.

Reduce Motion disables the shrink and projectile animation and makes wheels settle immediately; the static reminder and optional completion sound remain. Reduce Transparency keeps the fruit opaque. Haptics use AppKit's current-input-device performer and may do nothing on unsupported hardware or when system preferences disable them.

## Data and removal

No account, network traffic, telemetry, screen recording, Accessibility permission or input monitoring is needed. Settings live in `~/Library/Application Support/TomatoFocus/state.json`. The deadline is an absolute timestamp, so sleeping the Mac or quitting the app does not restart the duration. An overdue saved timer shows its reminder on reopening. The app cannot wake a sleeping or powered-off Mac.

To uninstall, turn off **Launch at login** in Preferences, quit through the menu bar and remove the app. Settings are retained; remove that one settings directory only if you also want to erase them. The Windows and Mac editions use separate local settings; this release does not sync or import Windows settings automatically.

## Build and verification

On a Mac with Xcode command-line tools and Swift 5.9 or later:

```sh
bash scripts/build-macos.sh
```

Run this from the repository checkout. It runs domain tests, compiles both architectures, combines them into a universal binary, generates a Retina icon, ad-hoc signs and verifies the bundle, launches an isolated UI verification, checks an exact resource allowlist, and creates a DMG/ZIP with SHA-256 checksums in `dist/`. Tests and generated previews stay in `build/macos/`; developer verification code only runs with an explicit command-line switch and isolated state directory. No separate verification executable is shipped.

The Mac version is maintained in `macOS/VERSION`; Windows continues to use the root `VERSION`. CI exercises native launch on Apple Silicon and Intel runners. Login preference tests cover defaults, disabling, persistence, pending approval, OS errors and avoiding repeated registration. Only disposable GitHub runners exercise the real registration/removal API; ordinary local builds use a simulated login service and do not change the developer's login items. An actual logout/login session, subjective audio quality, Force Touch feedback, physical input, display refresh performance and all Spaces/full-screen/multiple-display configurations still require physical Mac acceptance testing. See [validation notes](VALIDATION.md).

The repository's [license](https://github.com/Dante9k/tommi/blob/main/LICENSE) applies. The three processed impact samples retain Kenney's CC0 attribution, bundled with the app.
