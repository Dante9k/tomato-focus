import AppKit
import SwiftUI
import TomatoCore

final class AppController: NSObject, NSApplicationDelegate, NSWindowDelegate, NSMenuDelegate {
    var state = TimerState()
    let feedback = Feedback()
    private(set) var loginItem: LoginItemController!
    private(set) var panel: TomatoPanel!
    private(set) var tomato: TomatoView!
    private var status: NSStatusItem!
    private var store: StateStore!
    private var tickTimer: Timer?, appearanceTimer: Timer?, saveTimer: Timer?, previewTimer: Timer?
    private var overlay: ThrowOverlay?
    private var settings: NSWindow?
    private var hasReportedSaveError = false
    private(set) var isAnimating = false
    private(set) var isAlarming = false
    var isFocusing: Bool { state.deadline != nil && !isAlarming }
    var verificationDirectory: URL?

    func applicationDidFinishLaunching(_ notification: Notification) {
        let directory = verificationDirectory ?? FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("TomatoFocus", isDirectory: true)
        store = StateStore(url: directory.appendingPathComponent("state.json"))
        do { state = try store.load() }
        catch {
            // Preserve an unreadable file before allowing a fresh state to replace it.
            do { try FileManager.default.copyItem(at: store.url, to: directory.appendingPathComponent("state-unreadable-\(UUID().uuidString).json")) }
            catch { showError(localized("Saved settings could not be read or backed up. The app will close to preserve them.", "无法读取或备份设置。程序将退出以保留原文件。")); NSApp.terminate(nil); return }
            showError(localized("Saved settings were unreadable. A backup has been kept beside the settings file.", "设置文件无法读取，已在原目录保留备份。"))
        }
        loginItem = LoginItemController(service: verificationDirectory == nil ? MacLoginItemService() : VerificationLoginItemService())
        loginItem.initialize(state: &state)
        save()
        let editingSize = WidgetLayout.editingSize
        panel = TomatoPanel(contentRect: NSRect(x: 0, y: 0, width: editingSize, height: editingSize), styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        panel.title = "朱果 · Tomato Focus"; panel.backgroundColor = .clear; panel.isOpaque = false
        panel.hasShadow = false; panel.level = NSWindow.Level(rawValue: NSWindow.Level.floating.rawValue + 1)
        panel.hidesOnDeactivate = false; panel.isReleasedWhenClosed = false; panel.animationBehavior = .none
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]; panel.delegate = self
        tomato = TomatoView(frame: NSRect(x: 0, y: 0, width: editingSize, height: editingSize)); tomato.controller = self
        tomato.autoresizingMask = [.width, .height]; panel.contentView = tomato
        tomato.setDuration(state.duration)
        let screen = NSScreen.main?.visibleFrame ?? NSRect(x: 0, y: 0, width: 1440, height: 900)
        let savedSize = Double(WidgetLayout.savedCoordinateSize), size = Double(editingSize)
        panel.setFrameOrigin(NSPoint(x: state.x.map { $0 + savedSize - size } ?? Double(screen.maxX) - size - 30,
                                    y: state.y.map { $0 + savedSize - size } ?? Double(screen.maxY) - size - 30))
        ensureVisible()
        status = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        status.button?.image = Artwork.tomato.copy() as? NSImage; status.button?.image?.size = NSSize(width: 23, height: 23)
        status.button?.toolTip = "朱果 · Tomato Focus"
        let menu = NSMenu(); menu.delegate = self; status.menu = menu; rebuildMenu(menu)
        if state.deadline != nil {
            tomato.remaining = state.remaining()
            if tomato.remaining > 0 { tomato.setMode(focus: true, alarm: false); animate(focus: true, immediate: true) }
            else { alarm() }
        }
        panel.orderFrontRegardless()
        tickTimer = Timer.scheduledTimer(withTimeInterval: 0.2, repeats: true) { [weak self] _ in self?.tick() }
        NSWorkspace.shared.notificationCenter.addObserver(self, selector: #selector(woke), name: NSWorkspace.didWakeNotification, object: nil)
        NSWorkspace.shared.notificationCenter.addObserver(self, selector: #selector(accessibilityChanged), name: NSWorkspace.accessibilityDisplayOptionsDidChangeNotification, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(screensChanged), name: NSApplication.didChangeScreenParametersNotification, object: nil)
        if let directory = verificationDirectory { UIVerification.run(controller: self, directory: directory) }
    }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool { show(); return true }
    func applicationDidBecomeActive(_ notification: Notification) { loginItem?.refresh() }
    func applicationWillTerminate(_ notification: Notification) {
        saveTimer?.invalidate(); tickTimer?.invalidate(); appearanceTimer?.invalidate(); previewTimer?.invalidate()
        overlay?.stop(); feedback.stop(); savePosition()
    }
    @objc private func woke() { tick() }
    @objc private func accessibilityChanged() {
        animate(focus: isFocusing, immediate: true)
        tomato.wheels.forEach { $0.stop() }
    }
    @objc private func screensChanged() { ensureVisible(); if isAlarming { overlay?.stop(); makeOverlay() } }
    private func tick() {
        if isFocusing {
            let remaining = state.remaining()
            if remaining != tomato.remaining {
                tomato.remaining = remaining; tomato.needsDisplay = true
                tomato.setAccessibilityValue(localized("Remaining: ", "剩余：") + timeText(remaining))
            }
            if remaining == 0 { alarm() }
        }
        status?.button?.toolTip = isFocusing ? localized("Remaining: ", "剩余：") + timeText(state.remaining()) : "朱果 · Tomato Focus"
    }
    func edited() {
        guard !isFocusing && !isAlarming else { return }
        state.duration = max(1, tomato.duration)
        saveTimer?.invalidate(); saveTimer = Timer.scheduledTimer(withTimeInterval: 0.3, repeats: false) { [weak self] _ in self?.save() }
    }
    @objc func start() {
        guard !isFocusing && !isAlarming && !isAnimating else { return }
        tomato.wheels.forEach { $0.stop() }; state.duration = max(1, tomato.duration)
        state.start(); tomato.remaining = state.duration; savePosition()
        tomato.setMode(focus: true, alarm: false); feedback.stop(); animate(focus: true); refreshSettings()
    }
    @objc func cancel() {
        guard isFocusing else { return }
        state.cancel(); feedback.stop(); tomato.setDuration(state.duration)
        tomato.setMode(focus: false, alarm: false); animate(focus: false); save(); refreshSettings()
    }
    func alarm() {
        guard !isAlarming else { return }
        isAlarming = true; tomato.setMode(focus: false, alarm: true)
        animate(focus: false); panel.orderFrontRegardless(); makeOverlay()
        if state.completionSound { feedback.complete() }
        refreshSettings()
    }
    private func makeOverlay() {
        guard let screen = panel.screen ?? NSScreen.main else { return }
        overlay = ThrowOverlay(screen: screen, feedback: feedback, origin: { [weak self] in
            guard let self else { return .zero }
            // The stem follows the widget throughout its expansion animation.
            return NSPoint(x: self.panel.frame.minX + self.panel.frame.width * 0.51, y: self.panel.frame.maxY - self.panel.frame.height * 0.23)
        }, sound: { [weak self] in self?.state.effectsSound ?? false })
    }
    @objc func dismiss() {
        guard isAlarming else { return }
        previewTimer?.invalidate(); previewTimer = nil; overlay?.stop(); overlay = nil; feedback.stop()
        isAlarming = false; state.cancel(); tomato.setDuration(state.duration)
        tomato.setMode(focus: false, alarm: false); animate(focus: false); save(); refreshSettings()
    }
    func preview() {
        guard !isFocusing else { return }
        dismiss(); alarm()
        previewTimer = Timer.scheduledTimer(withTimeInterval: 8, repeats: false) { [weak self] _ in self?.dismiss() }
    }
    func applyPreset(_ seconds: Int) {
        guard !isFocusing else { return }; dismiss(); state.duration = seconds; tomato.setDuration(seconds); save(); show()
    }
    @objc func show() { tomato.resetGesture(); ensureVisible(); panel.orderFrontRegardless() }
    @objc func hide() { tomato.resetGesture(); panel.orderOut(nil) }
    @objc func quit() { NSApp.terminate(nil) }
    @objc func showSettings() {
        tomato.resetGesture()
        loginItem.refresh()
        if settings == nil {
            let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 476, height: 630), styleMask: [.titled, .closable, .fullSizeContentView], backing: .buffered, defer: false)
            window.title = localized("Tomato Focus · Preferences", "朱果 · 偏好设置")
            window.titlebarAppearsTransparent = true; window.titleVisibility = .hidden
            window.isReleasedWhenClosed = false; window.backgroundColor = NSColor(calibratedRed: 0.08, green: 0.16, blue: 0.14, alpha: 1)
            window.center(); settings = window
        }
        refreshSettings(); NSApp.activate(ignoringOtherApps: true); settings?.makeKeyAndOrderFront(nil)
    }
    func setLaunchAtLogin(_ enabled: Bool) {
        loginItem.setEnabled(enabled, state: &state)
        save()
    }
    private func refreshSettings() {
        if let settings {
            let host = NSHostingView(rootView: SettingsView(controller: self))
            settings.contentView = host
            settings.setContentSize(host.fittingSize)
        }
    }
    func menuWillOpen(_ menu: NSMenu) { rebuildMenu(menu) }
    private func rebuildMenu(_ menu: NSMenu) {
        menu.removeAllItems()
        let title = isAlarming ? localized("Time for a break", "该休息了") : isFocusing ? timeText(state.remaining()) : "朱果 · Tomato Focus"
        let heading = NSMenuItem(title: title, action: nil, keyEquivalent: ""); heading.isEnabled = false; menu.addItem(heading)
        menu.addItem(.separator())
        func item(_ title: String, _ selector: Selector, _ key: String = "") { let i = NSMenuItem(title: title, action: selector, keyEquivalent: key); i.target = self; menu.addItem(i) }
        item(localized("Show tomato", "显示番茄"), #selector(show))
        if isAlarming { item(localized("Dismiss reminder", "停止提醒"), #selector(dismiss)) }
        else if isFocusing { item(localized("Cancel focus", "取消专注"), #selector(cancel)); item(localized("Hide tomato", "隐藏番茄"), #selector(hide)) }
        else { item(localized("Start focus", "开始专注"), #selector(start)) }
        item(localized("Preferences…", "偏好设置…"), #selector(showSettings), ",")
        menu.addItem(.separator()); item(localized("Quit Tomato Focus", "退出朱果"), #selector(quit), "q")
    }
    private func animate(focus: Bool, immediate: Bool = false) {
        appearanceTimer?.invalidate(); tomato.resetGesture()
        let startFrame = panel.frame
        let targetSize = focus ? WidgetLayout.focusSize : isAlarming ? WidgetLayout.reminderSize : WidgetLayout.editingSize
        let anchor = NSPoint(x: startFrame.maxX, y: startFrame.maxY)
        let startOpacity = tomato.fruitOpacity
        let targetOpacity = focus && !NSWorkspace.shared.accessibilityDisplayShouldReduceTransparency ? 0.32 : 1.0
        let began = ProcessInfo.processInfo.systemUptime
        isAnimating = true; tomato.wheels.forEach { $0.isHidden = true }
        func frame(_ progress: Double) {
            let eased = 1 - pow(1 - progress, 4), size = startFrame.width + (targetSize - startFrame.width) * eased
            self.tomato.fruitOpacity = startOpacity + (targetOpacity - startOpacity) * eased
            self.panel.setFrame(NSRect(x: anchor.x - size, y: anchor.y - size, width: size, height: size), display: true)
            self.tomato.needsDisplay = true
        }
        func finish() {
            self.isAnimating = false; self.appearanceTimer = nil
            self.tomato.wheels.forEach { $0.isHidden = self.isFocusing || self.isAlarming }
        }
        if immediate || Artwork.reducedMotion { frame(1); finish(); return }
        appearanceTimer = Timer(timeInterval: 1.0 / 60, repeats: true) { timer in
            let p = min(1, (ProcessInfo.processInfo.systemUptime - began) / 0.46)
            frame(p)
            if p >= 1 { timer.invalidate(); finish() }
        }
        RunLoop.main.add(appearanceTimer!, forMode: .common)
    }
    func savePosition() {
        guard panel != nil else { return }
        state.x = Double(panel.frame.maxX - WidgetLayout.savedCoordinateSize)
        state.y = Double(panel.frame.maxY - WidgetLayout.savedCoordinateSize); save()
    }
    func save() {
        guard store != nil else { return }
        do { try store.save(state) }
        catch { if !hasReportedSaveError { hasReportedSaveError = true; showError(localized("Your settings could not be saved. Check free space and folder permissions.", "无法保存设置，请检查磁盘空间与目录权限。")) } }
    }
    private func ensureVisible() {
        guard panel != nil else { return }
        let frames = NSScreen.screens.map(\.visibleFrame)
        if frames.contains(where: { $0.intersection(panel.frame).width >= 60 && $0.intersection(panel.frame).height >= 60 }) { return }
        if let frame = frames.first { panel.setFrameOrigin(NSPoint(x: frame.maxX - panel.frame.width - 30, y: frame.maxY - panel.frame.height - 30)) }
    }
    private func timeText(_ seconds: Int) -> String { String(format: "%02d:%02d:%02d", seconds / 3600, seconds / 60 % 60, seconds % 60) }
    private func showError(_ text: String) { let alert = NSAlert(); alert.messageText = "Tomato Focus"; alert.informativeText = text; alert.runModal() }
}
