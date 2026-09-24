import XCTest
@testable import TomatoCore

final class CoreTests: XCTestCase {
    func testDeadlineSurvivesSleepAndRelaunch() throws {
        let dir = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: dir) }
        let store = StateStore(url: dir.appendingPathComponent("state.json"))
        var state = TimerState(); state.duration = 60
        let now = Date(timeIntervalSince1970: 10000)
        state.start(at: now); state.x = -250; state.wheelSound = false
        try store.save(state)
        let loaded = try store.load()
        XCTAssertEqual(loaded, state)
        XCTAssertEqual(loaded.remaining(at: now.addingTimeInterval(21.2)), 39)
        XCTAssertEqual(loaded.remaining(at: now.addingTimeInterval(100)), 0)
        state.cancel(); XCTAssertNil(state.deadline); XCTAssertEqual(state.duration, 60)
    }
    func testSettingsDefaultsAndDurationBounds() throws {
        let state = try JSONDecoder().decode(TimerState.self, from: Data("{\"duration\":999999}".utf8))
        XCTAssertEqual(state.duration, 86399); XCTAssertTrue(state.wheelSound)
    }
    func testShakeRequiresThreeLargeReversals() {
        var s = ShakeDetector(); s.reset()
        XCTAssertFalse(s.move(x: 40, y: 2, time: 0))
        XCTAssertFalse(s.move(x: 19, y: 3, time: 0.2))
        XCTAssertFalse(s.move(x: 42, y: 5, time: 0.4))
        XCTAssertTrue(s.move(x: 18, y: 7, time: 0.6))
        XCTAssertFalse(s.move(x: 44, y: 0, time: 0.8))
        s.reset(); XCTAssertFalse(s.move(x: 40, y: 0, time: 1))
    }
    func testOrdinaryDraggingJitterAndSlowTurnsDoNotCancel() {
        var s = ShakeDetector()
        for i in 0...100 { XCTAssertFalse(s.move(x: Double(i), y: Double(i % 5), time: Double(i) / 100)) }
        s.reset()
        for i in 0...8 { XCTAssertFalse(s.move(x: Double(i % 2) * 35, y: 0, time: Double(i))) }
        s.reset()
        for i in 0...30 { XCTAssertFalse(s.move(x: Double(i % 2) * 15, y: 0, time: Double(i) / 100)) }
    }
    func testVerticalShake() {
        var s = ShakeDetector()
        for (i, y) in [30.0, 0, 30].enumerated() { XCTAssertFalse(s.move(x: 0, y: y, time: Double(i) / 10)) }
        XCTAssertTrue(s.move(x: 0, y: 0, time: 0.3))
    }
    func testWheelWrapMultiStepAndSettling() {
        var wheel = WheelModel(count: 60); wheel.set(59)
        XCTAssertEqual(wheel.move(1), 1); XCTAssertEqual(wheel.value, 0)
        XCTAssertEqual(wheel.move(-2), -2); XCTAssertEqual(wheel.value, 58)
        XCTAssertEqual(wheel.move(4.2), 4); wheel.release(velocity: 20)
        for _ in 0..<180 { _ = wheel.step(1.0 / 60, reduceMotion: false) }
        XCTAssertFalse(wheel.isMoving)
        _ = wheel.move(0.3); _ = wheel.step(0.016, reduceMotion: true)
        XCTAssertFalse(wheel.isMoving)
    }
    func testBallisticLandingOneBounceAndBoundedLifetime() {
        var f = Flight(x: 400, y: 60, vx: 0, vy: -100, size: 70, spin: 1)
        var impacts = 0, alive = true
        for _ in 0..<720 where alive {
            alive = f.step(1.0 / 120, width: 1000, height: 800)
            if f.impacted { impacts += 1; XCTAssertGreaterThan(f.impactSpeed, 0); XCTAssertLessThanOrEqual(f.y + f.size * 0.39, 792.001) }
        }
        XCTAssertEqual(impacts, 2); XCTAssertEqual(f.bounces, 1); XCTAssertFalse(alive)
    }
}
