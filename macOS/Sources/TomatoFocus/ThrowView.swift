import AppKit
import TomatoCore

final class ThrowView: NSView {
    var flights: [Flight] = []
    override var isFlipped: Bool { true }
    override func draw(_ dirtyRect: NSRect) {
        guard let context = NSGraphicsContext.current?.cgContext else { return }
        context.clear(bounds)
        for flight in flights {
            context.saveGState(); context.translateBy(x: flight.x, y: flight.y); context.rotate(by: flight.angle)
            Artwork.tomato.draw(in: NSRect(x: -flight.size / 2, y: -flight.size / 2, width: flight.size, height: flight.size), from: .zero, operation: .sourceOver, fraction: flight.resting ? max(0, 1 - flight.restingAge / 0.32) : 1, respectFlipped: true, hints: nil)
            context.restoreGState()
        }
    }
}

final class ThrowOverlay {
    let panel: NSPanel
    let view: ThrowView
    private var timer: Timer?
    private var nextLaunch = 0.0, previous = 0.0
    private let origin: () -> NSPoint
    private let feedback: Feedback
    var sound: () -> Bool
    init(screen: NSScreen, feedback: Feedback, origin: @escaping () -> NSPoint, sound: @escaping () -> Bool) {
        self.origin = origin; self.feedback = feedback; self.sound = sound
        panel = NSPanel(contentRect: screen.frame, styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        panel.backgroundColor = .clear; panel.isOpaque = false; panel.hasShadow = false
        panel.level = .floating; panel.ignoresMouseEvents = true; panel.hidesOnDeactivate = false
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .ignoresCycle]
        view = ThrowView(frame: NSRect(origin: .zero, size: screen.frame.size)); panel.contentView = view
        panel.orderFrontRegardless()
        previous = ProcessInfo.processInfo.systemUptime; nextLaunch = previous
        timer = Timer(timeInterval: 1.0 / 60, repeats: true) { [weak self] _ in self?.tick() }
        RunLoop.main.add(timer!, forMode: .common)
    }
    func stop() { timer?.invalidate(); timer = nil; view.flights.removeAll(); panel.orderOut(nil) }
    private func tick() {
        let now = ProcessInfo.processInfo.systemUptime
        var elapsed = min(0.1, max(0, now - previous)); previous = now
        if Artwork.reducedMotion { view.flights.removeAll(); view.needsDisplay = true; return }
        if now >= nextLaunch && view.flights.count < 32 {
            nextLaunch = now + 0.23
            let global = origin(), x = global.x - panel.frame.minX, y = panel.frame.maxY - global.y
            let target = Double.random(in: 40...max(41, view.bounds.width - 40))
            let vy = -Double.random(in: 250...470)
            let floorDistance = max(0, view.bounds.height - y - 40)
            let landingTime = (-vy + sqrt(vy * vy + 1840 * floorDistance)) / 920
            view.flights.append(Flight(x: x, y: y, vx: (target - x) / max(0.3, landingTime), vy: vy, size: Double.random(in: 44...78), spin: Double.random(in: -6...6)))
            if sound() { feedback.launch(pan: x / view.bounds.width * 2 - 1) }
        }
        // Substeps keep collision timing stable when a display frame is delayed.
        while elapsed > 0 {
            let dt = min(1.0 / 120, elapsed); elapsed -= dt
            var alive: [Flight] = []
            for var flight in view.flights {
                let keep = flight.step(dt, width: view.bounds.width, height: view.bounds.height)
                if flight.impacted && sound() {
                    feedback.impact(speed: flight.impactSpeed, size: flight.size, settling: flight.resting, pan: flight.x / view.bounds.width * 2 - 1)
                }
                if keep { alive.append(flight) }
            }
            view.flights = alive
        }
        view.needsDisplay = true
    }
    deinit { timer?.invalidate() }
}
