import SwiftUI

struct SettingsView: View {
    let controller: AppController
    @ObservedObject private var loginItem: LoginItemController
    @State private var wheel: Bool
    @State private var completion: Bool
    @State private var effects: Bool
    @State private var haptics: Bool
    @State private var previewing = false
    private let cream = Color(red: 0.96, green: 0.94, blue: 0.87)
    private let coral = Color(red: 1, green: 0.40, blue: 0.33)
    init(controller: AppController) {
        self.controller = controller
        _loginItem = ObservedObject(wrappedValue: controller.loginItem)
        _wheel = State(initialValue: controller.state.wheelSound)
        _completion = State(initialValue: controller.state.completionSound)
        _effects = State(initialValue: controller.state.effectsSound)
        _haptics = State(initialValue: controller.state.haptics)
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 22) {
            HStack(spacing: 14) {
                Image(nsImage: Artwork.tomato).resizable().interpolation(.high).frame(width: 76, height: 76)
                VStack(alignment: .leading, spacing: 6) {
                    Text(localized("A little room to focus.", "留一点时间，给专注。"))
                        .font(.system(size: 21, weight: .semibold, design: .rounded))
                    Text("朱果 · TOMATO FOCUS").font(.system(size: 10, weight: .medium)).tracking(2).opacity(0.48)
                }
            }
            VStack(alignment: .leading, spacing: 12) {
                caption(localized("YOUR NEXT MOMENT", "下一段时光"))
                HStack(spacing: 10) {
                    preset(25, localized("Focus", "专注"))
                    preset(5, localized("Short break", "短休息"))
                    preset(15, localized("Long break", "长休息"))
                }
                if controller.isFocusing {
                    Text(localized("Your timer is running. Presets unlock when it ends.", "正在专注，结束后可以调整预设。"))
                        .font(.system(size: 11)).opacity(0.58)
                }
            }
            VStack(alignment: .leading, spacing: 14) {
                caption(localized("SMALL DETAILS", "细微的感受"))
                setting(localized("Wheel clicks", "拨轮卡点声"), subtitle: localized("A soft mechanical touch", "轻柔、短促的机械触感"), value: $wheel)
                setting(localized("Completion chime", "到时提示音"), subtitle: localized("Once, when your moment is complete", "在专注完成时轻响一次"), value: $completion)
                setting(localized("Throw & landing", "投掷与落地"), subtitle: localized("Sound follows each little tomato", "声音跟随每一颗小番茄"), value: $effects)
                setting(localized("Trackpad haptics", "触控板轻触反馈"), subtitle: localized("On compatible Force Touch devices", "由兼容的 Force Touch 设备提供"), value: $haptics)
                setting(localized("Launch at login", "登录时启动"), subtitle: loginItem.detail,
                        value: Binding(get: { loginItem.isEnabled }, set: { controller.setLaunchAtLogin($0) }))
                if loginItem.needsAttention {
                    HStack(spacing: 18) {
                        Button(localized("Login Items…", "打开系统登录项…")) { loginItem.openSystemSettings() }
                        if loginItem.errorMessage != nil || loginItem.status == .notFound {
                            Button(localized("Try again", "重试")) { controller.setLaunchAtLogin(loginItem.requestedEnabled) }
                        }
                    }.font(.system(size: 11)).buttonStyle(.plain).foregroundStyle(coral)
                }
            }
            .padding(18).frame(maxWidth: .infinity, alignment: .leading)
            .background(.white.opacity(0.045), in: RoundedRectangle(cornerRadius: 18))
            HStack {
                Button(previewing ? localized("Stop preview", "停止预览") : localized("Try the reminder", "看看休息提醒")) {
                    if previewing { controller.dismiss(); previewing = false }
                    else { controller.preview(); previewing = true; DispatchQueue.main.asyncAfter(deadline: .now() + 8) { previewing = false } }
                }.buttonStyle(.plain).foregroundStyle(coral).disabled(controller.isFocusing)
                Spacer()
                Text("macOS · \(Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "")")
                    .font(.system(size: 10)).opacity(0.35)
            }
            Text(localized("Double-click the stem to start. Shake the tomato to cancel.\nEverything stays on this Mac.", "双击绿蒂开始，摇晃番茄取消。\n所有数据只保存在这台 Mac 上。"))
                .font(.system(size: 11)).lineSpacing(4).opacity(0.45)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(28).frame(width: 420).fixedSize(horizontal: false, vertical: true).foregroundStyle(cream)
        .background(LinearGradient(colors: [Color(red: 0.08, green: 0.16, blue: 0.14), Color(red: 0.035, green: 0.09, blue: 0.08)], startPoint: .topLeading, endPoint: .bottomTrailing))
        .tint(coral).preferredColorScheme(.dark)
        .onChange(of: wheel) { controller.state.wheelSound = $0; controller.save() }
        .onChange(of: completion) { controller.state.completionSound = $0; controller.save() }
        .onChange(of: effects) { controller.state.effectsSound = $0; if !$0 { controller.feedback.stop() }; controller.save() }
        .onChange(of: haptics) { controller.state.haptics = $0; controller.save() }
    }
    private func caption(_ text: String) -> some View { Text(text).font(.system(size: 10, weight: .semibold)).tracking(1.5).opacity(0.45) }
    private func preset(_ minutes: Int, _ name: String) -> some View {
        Button { controller.applyPreset(minutes * 60) } label: {
            VStack(alignment: .leading, spacing: 6) {
                Text("\(minutes)").font(.system(size: 26, weight: .medium, design: .rounded))
                Text(name).font(.system(size: 10)).opacity(0.55)
            }.frame(maxWidth: .infinity, alignment: .leading).padding(14)
                .background(.white.opacity(0.055), in: RoundedRectangle(cornerRadius: 14))
        }.buttonStyle(.plain).disabled(controller.isFocusing)
    }
    private func setting(_ title: String, subtitle: String, value: Binding<Bool>) -> some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(title).font(.system(size: 13, weight: .medium))
                Text(subtitle).font(.system(size: 10)).opacity(0.45).fixedSize(horizontal: false, vertical: true)
            }
            Spacer(minLength: 12)
            Toggle(title, isOn: value).labelsHidden().toggleStyle(.switch).controlSize(.small)
        }
    }
}
