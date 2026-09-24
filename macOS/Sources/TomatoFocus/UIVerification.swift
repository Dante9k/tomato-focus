import AppKit
import TomatoCore

/// Runs only with --verify-ui and an explicit isolated directory; never touches user settings.
enum UIVerification {
    static func run(controller c: AppController, directory: URL) {
        func require(_ condition: @autoclosure () -> Bool, _ message: String) {
            if !condition() { fputs("UI verification failed: \(message)\n", stderr); exit(1) }
        }
        func capture(_ name: String) {
            c.tomato.displayIfNeeded()
            guard let bitmap = c.tomato.bitmapImageRepForCachingDisplay(in: c.tomato.bounds) else { require(false, "No bitmap"); return }
            c.tomato.cacheDisplay(in: c.tomato.bounds, to: bitmap)
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
        DispatchQueue.main.asyncAfter(deadline: .now() + 3.6) { c.start(); c.cancel() }
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
