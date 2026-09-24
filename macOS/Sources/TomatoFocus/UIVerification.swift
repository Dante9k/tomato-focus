import AppKit
import TomatoCore

/// Runs only with --verify-ui and an explicit isolated directory; never touches user settings.
enum UIVerification {
    static func run(controller c: AppController, directory: URL) {
        func require(_ condition: @autoclosure () -> Bool, _ message: String) {
            if !condition() { fputs("UI verification failed: \(message)\n", stderr); exit(1) }
        }
        DispatchQueue.global().asyncAfter(deadline: .now() + 15) {
            fputs("UI verification timed out\n", stderr); exit(1)
        }
        if c.state.deadline != nil {
            let overdue = c.state.remaining() == 0
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.7) {
                if overdue {
                    require(c.isAlarming && c.panel.frame.width == 250, "Overdue deadline must restore reminder")
                    c.dismiss(); require(c.state.deadline == nil, "Restored reminder dismissal")
                } else {
                    require(c.isFocusing && c.panel.frame.width == 125 && c.state.remaining() <= 30, "Resume original deadline")
                    require(c.tomato.wheels.allSatisfy(\.isHidden), "Restored focus is read-only")
                }
                do {
                    try "PASS: \(overdue ? "overdue reminder restoration" : "active deadline restoration")\n".write(to: directory.appendingPathComponent("restore-results.txt"), atomically: true, encoding: .utf8)
                } catch { require(false, "Restoration report") }
                NSApp.terminate(nil)
            }
            return
        }
        func capture(_ name: String, view: NSView? = nil) {
            let view = view ?? c.tomato!
            view.displayIfNeeded()
            guard let bitmap = view.bitmapImageRepForCachingDisplay(in: view.bounds) else { require(false, "No bitmap"); return }
            view.cacheDisplay(in: view.bounds, to: bitmap)
            if name == "macos-focus" {
                let alpha = bitmap.colorAt(x: bitmap.pixelsWide / 2, y: bitmap.pixelsHigh / 2)?.alphaComponent ?? 1
                require(alpha > 0.25 && alpha < 0.4, "Actual fruit pixels must remain translucent across redraws")
            }
            do {
                try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
                guard let data = bitmap.representation(using: .png, properties: [:]) else { require(false, "No PNG"); return }
                require(data.count > 3000, "Render is unexpectedly empty")
                try data.write(to: directory.appendingPathComponent(name + ".png"))
            } catch { require(false, "Could not save preview") }
        }
        require(c.feedback.ready, "Audio resources did not preload")
        c.state.wheelSound = false; c.state.completionSound = false; c.state.effectsSound = false; c.state.haptics = false
        c.applyPreset(60); require(c.tomato.duration == 60, "Preset")
        _ = c.tomato.wheels[2].accessibilityPerformIncrement()
        require(c.tomato.duration == 61 && c.state.duration == 61, "Wheel event")
        c.applyPreset(2); capture("macos-edit")
        let anchor = NSPoint(x: c.panel.frame.maxX, y: c.panel.frame.maxY)
        c.start()
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.7) {
            require(c.isFocusing && c.panel.frame.width == 125, "Focus size/state")
            require(c.tomato.wheels.allSatisfy(\.isHidden), "Editor must be hidden")
            require(abs(c.panel.frame.maxX - anchor.x) < 0.1 && abs(c.panel.frame.maxY - anchor.y) < 0.1, "Upper-right anchor")
            require(c.tomato.fruitOpacity <= 0.33 || NSWorkspace.shared.accessibilityDisplayShouldReduceTransparency, "Focus opacity")
            // Hosted Macs may enable Reduce Transparency. Exercise the normal 32% renderer too,
            // without changing that machine's accessibility preferences.
            c.tomato.fruitOpacity = 0.32; c.tomato.needsDisplay = true
            capture("macos-focus")
            c.hide(); require(!c.panel.isVisible && c.isFocusing, "Hidden timer continues")
            c.show(); require(c.panel.isVisible, "Restore from menu")
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 3) {
            require(c.isAlarming && c.panel.frame.width == 250, "Deadline reminder")
            require(abs(c.panel.frame.maxX - anchor.x) < 0.1, "Reminder must not jump to screen corner")
            capture("macos-reminder"); c.dismiss()
            require(!c.isAlarming && c.state.deadline == nil, "Dismiss reminder")
            c.applyPreset(90)
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 3.6) {
            c.showSettings()
            if let settings = NSApp.windows.first(where: { $0 !== c.panel && $0.styleMask.contains(.titled) }), let view = settings.contentView {
                view.layoutSubtreeIfNeeded(); capture("macos-settings", view: view); settings.orderOut(nil)
            } else { require(false, "Preferences window") }
            c.start(); c.cancel()
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 4.3) {
            require(!c.isFocusing && c.panel.frame.width == 250 && c.tomato.duration == 90, "Cancel restores editor")
            do {
                let persisted = try StateStore(url: directory.appendingPathComponent("state.json")).load()
                require(persisted.deadline == nil && persisted.duration == 90, "Persisted state")
                try "PASS: preset, wheel event, audio preload, focus/opacity/anchor, hide/restore, expiry, dismissal, cancellation, persistence\n".write(to: directory.appendingPathComponent("ui-results.txt"), atomically: true, encoding: .utf8)
            } catch { require(false, "State verification") }
            print("macOS UI verification passed")
            NSApp.terminate(nil)
        }
    }
}
