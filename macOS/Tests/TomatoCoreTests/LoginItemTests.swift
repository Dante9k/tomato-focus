import XCTest
@testable import TomatoCore

final class LoginItemTests: XCTestCase {
    final class Service: LoginItemService {
        var status: LoginItemStatus = .notRegistered
        var registrations = 0, removals = 0
        var requiresApproval = false, denied = false
        func register() throws {
            registrations += 1
            if denied { throw NSError(domain: "LoginItemTest", code: 1) }
            status = requiresApproval ? .requiresApproval : .enabled
        }
        func unregister() throws {
            removals += 1
            if denied { throw NSError(domain: "LoginItemTest", code: 1) }
            status = .notRegistered
        }
    }
    func testDefaultIsAppliedOnceIncludingOldSettings() throws {
        var state = try JSONDecoder().decode(TimerState.self, from: Data("{\"duration\":1500}".utf8))
        let service = Service()
        XCTAssertTrue(state.launchAtLogin); XCTAssertFalse(state.loginItemInitialized)
        try LoginItemPolicy.initialize(state: &state, service: service)
        XCTAssertEqual(service.registrations, 1); XCTAssertTrue(state.loginItemInitialized)
        service.status = .notRegistered // The user removes the item in System Settings.
        try LoginItemPolicy.initialize(state: &state, service: service)
        XCTAssertEqual(service.registrations, 1)
        XCTAssertEqual(service.status, .notRegistered)
    }
    func testDisabledPreferenceSurvivesPersistenceAndRelaunch() throws {
        var state = TimerState(); let service = Service()
        try LoginItemPolicy.initialize(state: &state, service: service)
        try LoginItemPolicy.setEnabled(false, state: &state, service: service)
        var loaded = try JSONDecoder().decode(TimerState.self, from: JSONEncoder().encode(state))
        try LoginItemPolicy.initialize(state: &loaded, service: service)
        XCTAssertFalse(loaded.launchAtLogin)
        XCTAssertEqual(service.removals, 1); XCTAssertEqual(service.registrations, 1)
    }
    func testApprovalDoesNotCauseRepeatedRegistration() throws {
        var state = TimerState(); let service = Service(); service.requiresApproval = true
        try LoginItemPolicy.initialize(state: &state, service: service)
        try LoginItemPolicy.setEnabled(true, state: &state, service: service)
        XCTAssertEqual(service.status, .requiresApproval); XCTAssertEqual(service.registrations, 1)
        try LoginItemPolicy.setEnabled(false, state: &state, service: service)
        XCTAssertEqual(service.status, .notRegistered)
    }
    func testFailureDoesNotPretendSuccessfulDisableOrRetryAtEveryLaunch() throws {
        var state = TimerState(); let service = Service(); service.denied = true
        XCTAssertThrowsError(try LoginItemPolicy.initialize(state: &state, service: service))
        try LoginItemPolicy.initialize(state: &state, service: service)
        XCTAssertEqual(service.registrations, 1)
        service.denied = false
        try LoginItemPolicy.setEnabled(true, state: &state, service: service)
        service.denied = true
        XCTAssertThrowsError(try LoginItemPolicy.setEnabled(false, state: &state, service: service))
        XCTAssertTrue(state.launchAtLogin); XCTAssertEqual(service.status, .enabled)
    }
}
