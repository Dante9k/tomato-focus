import AppKit

@main
enum Main {
    static func main() {
        let args = CommandLine.arguments
        let app = NSApplication.shared
        if !args.contains("--verify-ui"), let id = Bundle.main.bundleIdentifier,
           let existing = NSRunningApplication.runningApplications(withBundleIdentifier: id).first(where: { $0.processIdentifier != ProcessInfo.processInfo.processIdentifier }) {
            existing.activate(options: [.activateIgnoringOtherApps]); return
        }
        let controller = AppController()
        if let index = args.firstIndex(of: "--verify-ui"), args.count > index + 1 {
            controller.verificationDirectory = URL(fileURLWithPath: args[index + 1], isDirectory: true)
        }
        app.setActivationPolicy(.accessory); app.delegate = controller
        withExtendedLifetime(controller) { app.run() }
    }
}
