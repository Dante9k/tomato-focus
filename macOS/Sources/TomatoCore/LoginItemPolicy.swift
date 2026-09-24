import Foundation

public enum LoginItemStatus: Equatable {
    case notRegistered, enabled, requiresApproval, notFound
}

public protocol LoginItemService: AnyObject {
    var status: LoginItemStatus { get }
    func register() throws
    func unregister() throws
}

public enum LoginItemPolicy {
    /// Apply the default once. Subsequent launches must respect changes made in System Settings.
    public static func initialize(state: inout TimerState, service: LoginItemService) throws {
        guard !state.loginItemInitialized else { return }
        state.loginItemInitialized = true
        try setEnabled(state.launchAtLogin, state: &state, service: service)
    }

    public static func setEnabled(_ enabled: Bool, state: inout TimerState, service: LoginItemService) throws {
        if enabled {
            if service.status != .enabled && service.status != .requiresApproval { try service.register() }
        } else if service.status == .enabled || service.status == .requiresApproval {
            try service.unregister()
        }
        // Persist only successful changes; an OS denial must not pretend the preference took effect.
        state.launchAtLogin = enabled
        state.loginItemInitialized = true
    }
}
