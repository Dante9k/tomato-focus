import Foundation

public struct ShakeDetector {
    private var startX = 0.0, startY = 0.0, extreme = 0.0
    private var axis = 0, direction = 0.0
    private var turns: [Double] = []
    private var triggered = false
    public init() {}
    public mutating func reset(x: Double = 0, y: Double = 0) {
        startX = x; startY = y; extreme = 0; axis = 0; direction = 0
        turns.removeAll(keepingCapacity: true); triggered = false
    }
    public mutating func move(x: Double, y: Double, time: Double) -> Bool {
        guard !triggered else { return false }
        if axis == 0 {
            let dx = x - startX, dy = y - startY
            guard max(abs(dx), abs(dy)) >= 20 else { return false }
            axis = abs(dx) >= abs(dy) ? 1 : 2
            direction = (axis == 1 ? dx : dy) >= 0 ? 1 : -1
            extreme = axis == 1 ? x : y
            return false
        }
        let value = axis == 1 ? x : y
        if (value - extreme) * direction >= 0 { extreme = value }
        else if abs(value - extreme) >= 20 {
            direction = -direction; extreme = value
            turns.removeAll { time - $0 > 1 }
            turns.append(time)
            if turns.count >= 3 { triggered = true; return true }
        }
        return false
    }
}

/// Unbounded phase preserves wrapping and fractional travel; feedback is emitted only by user input.
public struct WheelModel {
    public let count: Int
    public private(set) var position: Double = 0
    public private(set) var velocity: Double = 0
    public var value: Int { let n = Int(position.rounded()); return (n % count + count) % count }
    public init(count: Int) { self.count = count }
    public mutating func set(_ value: Int) { position = Double((value % count + count) % count); velocity = 0 }
    @discardableResult public mutating func move(_ delta: Double) -> Int {
        let old = Int(position.rounded()); position += delta
        return Int(position.rounded()) - old
    }
    public mutating func release(velocity: Double) { self.velocity = min(30, max(-30, velocity)) }
    public mutating func stop() { velocity = 0; position = position.rounded() }
    public mutating func step(_ dt: Double, reduceMotion: Bool) -> Int {
        if reduceMotion { stop(); return 0 }
        let old = Int(position.rounded())
        if abs(velocity) > 0.4 {
            position += velocity * dt; velocity *= exp(-8 * dt)
        } else {
            velocity = 0
            position += (position.rounded() - position) * min(1, dt * 20)
            if abs(position.rounded() - position) < 0.001 { position = position.rounded() }
        }
        return Int(position.rounded()) - old
    }
    public var isMoving: Bool { velocity != 0 || abs(position - position.rounded()) > 0.001 }
}

public struct Flight {
    public var x: Double, y: Double, vx: Double, vy: Double, size: Double
    public var angle = 0.0, spin: Double
    public private(set) var age = 0.0, restingAge = 0.0, bounces = 0
    public private(set) var impacted = false, impactSpeed = 0.0, resting = false
    public init(x: Double, y: Double, vx: Double, vy: Double, size: Double, spin: Double) {
        self.x = x; self.y = y; self.vx = vx; self.vy = vy; self.size = size; self.spin = spin
    }
    // Screen-local coordinates have a downward positive y axis.
    public mutating func step(_ dt: Double, width: Double, height: Double) -> Bool {
        impacted = false; age += dt
        if resting { restingAge += dt; return restingAge < 0.32 && age < 6 }
        x += vx * dt; y += vy * dt + 0.5 * 920 * dt * dt; vy += 920 * dt; angle += spin * dt
        let radius = size * 0.39
        if y + radius >= height - 8 && vy > 0 {
            y = height - 8 - radius; impacted = true; impactSpeed = vy
            if bounces == 0 { vy *= -0.48; vx *= 0.77; spin *= 0.72; bounces = 1 }
            else { resting = true; vx = 0; vy = 0; spin = 0 }
        }
        return age < 6 && x > -2 * size && x < width + 2 * size && y < height + 2 * size
    }
}
