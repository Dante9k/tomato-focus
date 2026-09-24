import AppKit
import Combine
import ServiceManagement
import TomatoCore

final class MacLoginItemService: LoginItemService {
    var status: LoginItemStatus {
        switch SMAppService.mainApp.status {
        case .enabled: return .enabled
        case .requiresApproval: return .requiresApproval
        case .notFound: return .notFound
        case .notRegistered: return .notRegistered
        @unknown default: return .notFound
        }
    }
    func register() throws { try SMAppService.mainApp.register() }
    func unregister() throws { try SMAppService.mainApp.unregister() }
}

final class LoginItemController: ObservableObject {
    private let service: LoginItemService
    @Published private(set) var status: LoginItemStatus
    @Published private(set) var errorMessage: String?

    init(service: LoginItemService) { self.service = service; status = service.status }
    var isEnabled: Bool { status == .enabled || status == .requiresApproval }
    var needsAttention: Bool { status == .requiresApproval || status == .notFound || errorMessage != nil }
    var detail: String {
        if let errorMessage { return errorMessage }
        switch status {
        case .enabled: return localized("Your tomato will appear when you log in", "登录 Mac 时自动显示番茄")
        case .requiresApproval: return localized("Waiting for approval in System Settings", "等待在系统设置中批准")
        case .notRegistered: return localized("Open Tomato Focus yourself when needed", "需要时手动打开朱果")
        case .notFound: return localized("Move the app to Applications, then enable again", "请将程序移入应用程序目录后重新开启")
        }
    }
    func initialize(state: inout TimerState) {
        do { try LoginItemPolicy.initialize(state: &state, service: service) }
        catch { record(error) }
        refresh()
    }
    func setEnabled(_ enabled: Bool, state: inout TimerState) {
        errorMessage = nil
        do { try LoginItemPolicy.setEnabled(enabled, state: &state, service: service) }
        catch { record(error) }
        refresh()
    }
    func refresh() {
        let actual = service.status
        if actual != status { status = actual; errorMessage = nil }
    }
    func openSystemSettings() { SMAppService.openSystemSettingsLoginItems() }
    private func record(_ error: Error) {
        let code = (error as NSError).code
        errorMessage = localized("macOS could not change this setting (\(code)). Try again after installing the app.", "macOS 未能修改登录项（\(code)），请安装程序后重试。")
    }
}
