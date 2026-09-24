import AppKit
import TomatoCore

final class TimeWheel: NSView {
    var model: WheelModel
    var changed: (() -> Void)?
    var detent: (() -> Void)?
    var start: (() -> Void)?
    private var timer: Timer?, settle: Timer?
    private var lastY = 0.0, lastTime = 0.0, dragVelocity = 0.0, downY = 0.0
    private var typed = "", typeTime = 0.0
    override var isFlipped: Bool { true }
    override var acceptsFirstResponder: Bool { true }
    init(count: Int, label: String) {
        model = WheelModel(count: count)
        super.init(frame: NSRect(x: 0, y: 0, width: 48, height: 92))
        setAccessibilityElement(true); setAccessibilityRole(.slider); setAccessibilityLabel(label)
        setAccessibilityHelp(localized("Scroll, drag or use arrow keys to adjust", "滚动、拖动或用方向键调整"))
        toolTip = label; setAccessibilityValue(0)
    }
    required init?(coder: NSCoder) { fatalError("init(coder:) is unavailable") }
    func set(_ value: Int) { stop(); model.set(value); update(feedback: false) }
    func stop() { timer?.invalidate(); timer = nil; settle?.invalidate(); settle = nil; model.stop() }
    private func update(feedback: Bool) {
        needsDisplay = true; superview?.needsDisplay = true; setAccessibilityValue(model.value)
        if feedback { detent?(); changed?() }
    }
    override func draw(_ dirtyRect: NSRect) {
        NSGraphicsContext.saveGraphicsState(); NSBezierPath(rect: bounds).addClip()
        let center = Int(model.position.rounded()), fraction = model.position - Double(center)
        for offset in -2...2 {
            let distance = Double(offset) - fraction
            let value = ((center + offset) % model.count + model.count) % model.count
            let y = 33 + sin(distance * 0.56) * 43
            let alpha = max(0, 1 - abs(distance) * 0.43)
            let fontSize = 24 - min(6, abs(distance) * 4)
            Artwork.text(String(format: "%02d", value), rect: NSRect(x: 0, y: y, width: 48, height: 31), size: fontSize, color: Artwork.cream.withAlphaComponent(alpha), weight: .semibold)
        }
        if window?.firstResponder === self {
            Artwork.cream.withAlphaComponent(0.35).setStroke()
            let path = NSBezierPath(roundedRect: NSRect(x: 4, y: 31, width: 40, height: 30), xRadius: 7, yRadius: 7)
            path.lineWidth = 1; path.stroke()
        }
        NSGraphicsContext.restoreGraphicsState()
    }
    override func becomeFirstResponder() -> Bool { needsDisplay = true; return true }
    override func resignFirstResponder() -> Bool { needsDisplay = true; return true }
    override func scrollWheel(with event: NSEvent) {
        guard !isHidden else { return }
        window?.makeFirstResponder(self); timer?.invalidate(); timer = nil; settle?.invalidate()
        model.release(velocity: 0)
        let delta = -Double(event.scrollingDeltaY) / (event.hasPreciseScrollingDeltas ? 24 : 1)
        let steps = model.move(delta)
        if Artwork.reducedMotion { model.stop() }
        update(feedback: steps != 0)
        // AppKit supplies trackpad momentum; adding another inertia would double it.
        settle = Timer.scheduledTimer(withTimeInterval: 0.12, repeats: false) { [weak self] _ in self?.animate() }
    }
    override func mouseDown(with event: NSEvent) {
        stop(); window?.makeFirstResponder(self)
        lastY = convert(event.locationInWindow, from: nil).y; downY = lastY; lastTime = event.timestamp; dragVelocity = 0
    }
    override func mouseDragged(with event: NSEvent) {
        let y = convert(event.locationInWindow, from: nil).y
        let delta = (lastY - y) / 29, dt = max(0.001, event.timestamp - lastTime)
        dragVelocity = delta / dt
        let steps = model.move(delta); lastY = y; lastTime = event.timestamp
        if Artwork.reducedMotion { model.stop() }
        update(feedback: steps != 0)
    }
    override func mouseUp(with event: NSEvent) {
        let y = convert(event.locationInWindow, from: nil).y
        if abs(y - downY) < 3 {
            let direction = y < 31 ? -1 : y > 61 ? 1 : 0
            let steps = model.move(Double(direction)); update(feedback: steps != 0)
        } else if event.timestamp - lastTime < 0.1 && !Artwork.reducedMotion { model.release(velocity: dragVelocity) }
        animate()
    }
    private func animate() {
        timer?.invalidate()
        if Artwork.reducedMotion { model.stop(); update(feedback: false); return }
        var last = ProcessInfo.processInfo.systemUptime
        timer = Timer(timeInterval: 1.0 / 60, repeats: true) { [weak self] timer in
            guard let self else { timer.invalidate(); return }
            let now = ProcessInfo.processInfo.systemUptime
            let steps = self.model.step(min(0.05, now - last), reduceMotion: Artwork.reducedMotion); last = now
            self.update(feedback: steps != 0)
            if !self.model.isMoving { timer.invalidate(); self.timer = nil }
        }
        RunLoop.main.add(timer!, forMode: .common)
    }
    override func keyDown(with event: NSEvent) {
        if event.keyCode == 36 { start?(); return }
        if event.keyCode == 48 {
            if event.modifierFlags.contains(.shift) { window?.selectPreviousKeyView(self) } else { window?.selectNextKeyView(self) }
            return
        }
        if event.keyCode == 126 || event.keyCode == 125 {
            stop(); let steps = model.move(event.keyCode == 126 ? 1 : -1); update(feedback: steps != 0); return
        }
        if let s = event.characters, s.count == 1, let digit = Int(s) {
            if event.timestamp - typeTime > 1 { typed = "" }
            typeTime = event.timestamp; typed += String(digit)
            if typed.count > 2 || (Int(typed) ?? 0) >= model.count { typed = String(digit) }
            let old = model.value; model.set(Int(typed) ?? 0); update(feedback: old != model.value); return
        }
        super.keyDown(with: event)
    }
    override func accessibilityPerformIncrement() -> Bool { stop(); _ = model.move(1); update(feedback: true); return true }
    override func accessibilityPerformDecrement() -> Bool { stop(); _ = model.move(-1); update(feedback: true); return true }
}
