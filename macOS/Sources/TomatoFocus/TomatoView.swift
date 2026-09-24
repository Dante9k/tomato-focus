import AppKit
import TomatoCore

final class TomatoPanel: NSPanel {
    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { false }
}

final class TomatoView: NSView {
    weak var controller: AppController?
    let wheels = [TimeWheel(count: 24, label: localized("Hours", "小时")), TimeWheel(count: 60, label: localized("Minutes", "分钟")), TimeWheel(count: 60, label: localized("Seconds", "秒"))]
    var focusing = false, alarming = false
    var fruitOpacity = 1.0
    var remaining = 1500
    private var shake = ShakeDetector()
    private var down = NSPoint.zero, origin = NSPoint.zero
    private var dragging = false
    override var isFlipped: Bool { true }
    override var acceptsFirstResponder: Bool { true }
    override init(frame: NSRect) {
        super.init(frame: frame)
        toolTip = localized("Double-click the stem to focus · Drag to move · Shake to cancel", "双击绿蒂开始 · 拖动移动 · 专注时摇晃取消")
        for (i, wheel) in wheels.enumerated() {
            wheel.frame.origin = NSPoint(x: 53 + i * 48, y: 100)
            addSubview(wheel); wheel.nextKeyView = wheels[(i + 1) % 3]
            wheel.changed = { [weak self] in self?.controller?.edited() }
            wheel.detent = { [weak self] in
                guard let c = self?.controller else { return }
                c.feedback.detent(sound: c.state.wheelSound, haptics: c.state.haptics)
            }
            wheel.start = { [weak self] in self?.controller?.start() }
        }
        setAccessibilityLabel(localized("Tomato Focus", "朱果番茄钟"))
    }
    required init?(coder: NSCoder) { fatalError("init(coder:) is unavailable") }
    func setDuration(_ duration: Int) { wheels[0].set(duration / 3600); wheels[1].set(duration / 60 % 60); wheels[2].set(duration % 60) }
    var duration: Int { wheels[0].model.value * 3600 + wheels[1].model.value * 60 + wheels[2].model.value }
    func resetGesture() { shake.reset(); dragging = false }
    func setMode(focus: Bool, alarm: Bool) {
        focusing = focus; alarming = alarm; resetGesture()
        for wheel in wheels { wheel.stop(); wheel.isHidden = focus || alarm }
        window?.makeFirstResponder(self); needsDisplay = true
    }
    override func draw(_ dirtyRect: NSRect) {
        guard let context = NSGraphicsContext.current?.cgContext else { return }
        context.saveGState(); context.scaleBy(x: bounds.width / 250, y: bounds.height / 250)
        Artwork.tomato.draw(in: NSRect(x: 0, y: 0, width: 250, height: 250), from: .zero, operation: .sourceOver, fraction: fruitOpacity, respectFlipped: true, hints: [.interpolation: NSImageInterpolation.high])
        if focusing { Artwork.readout(remaining, in: context) }
        else if alarming {
            Artwork.text(localized("Take a breath", "休息一下"), rect: NSRect(x: 45, y: 130, width: 160, height: 30), size: 21, color: Artwork.cream, weight: .semibold)
            Artwork.text(localized("Drag to dismiss", "拖动即可结束提醒"), rect: NSRect(x: 40, y: 165, width: 170, height: 20), size: 10, color: Artwork.cream.withAlphaComponent(0.85))
        } else {
            let glass = NSBezierPath(roundedRect: NSRect(x: 38, y: 100, width: 174, height: 92), xRadius: 19, yRadius: 19)
            NSGradient(starting: NSColor(calibratedRed: 0.17, green: 0.03, blue: 0.04, alpha: 0.58), ending: NSColor(calibratedRed: 0.26, green: 0.04, blue: 0.06, alpha: 0.40))?.draw(in: glass, angle: 90)
            Artwork.cream.withAlphaComponent(0.17).setStroke(); glass.lineWidth = 0.8; glass.stroke()
            Artwork.cream.withAlphaComponent(0.065).setFill()
            NSBezierPath(roundedRect: NSRect(x: 46, y: 131, width: 158, height: 30), xRadius: 9, yRadius: 9).fill()
            for x in [94, 142] { Artwork.text(":", rect: NSRect(x: x, y: 133, width: 14, height: 30), size: 20, color: Artwork.cream.withAlphaComponent(0.65)) }
            Artwork.text("···", rect: NSRect(x: 112, y: 201, width: 26, height: 24), size: 19, color: Artwork.cream.withAlphaComponent(0.85))
        }
        context.restoreGState()
    }
    override func mouseDown(with event: NSEvent) {
        guard let window, controller?.isAnimating != true else { return }
        let local = convert(event.locationInWindow, from: nil)
        if !focusing && !alarming && local.y > 195 && local.y < 226 && abs(local.x - 125) < 22 { controller?.showSettings(); return }
        if event.clickCount == 2 {
            if alarming { controller?.dismiss() }
            else if !focusing { controller?.start() }
            return
        }
        down = NSEvent.mouseLocation; origin = window.frame.origin; dragging = true
        shake.reset(x: down.x, y: down.y); window.makeKey()
    }
    override func mouseDragged(with event: NSEvent) {
        guard dragging, let window else { return }
        let point = NSEvent.mouseLocation
        if alarming {
            controller?.dismiss(); dragging = true; origin = window.frame.origin; down = point
        }
        window.setFrameOrigin(NSPoint(x: origin.x + point.x - down.x, y: origin.y + point.y - down.y))
        if focusing && shake.move(x: point.x, y: point.y, time: event.timestamp) { controller?.cancel() }
    }
    override func mouseUp(with event: NSEvent) { resetGesture(); controller?.savePosition() }
    override func rightMouseDown(with event: NSEvent) { controller?.showSettings() }
    override func keyDown(with event: NSEvent) {
        if event.keyCode == 53 { if alarming { controller?.dismiss() }; return }
        if event.keyCode == 36 && !focusing && !alarming { controller?.start(); return }
        if event.keyCode == 48 && !focusing && !alarming { window?.makeFirstResponder(wheels[0]); return }
        super.keyDown(with: event)
    }
}
