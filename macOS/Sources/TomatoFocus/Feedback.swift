import AppKit
import AVFoundation

/// Players are prepared once; occupied voices are dropped rather than queued.
final class Feedback {
    private var clicks: [AVAudioPlayer] = [], launches: [AVAudioPlayer] = [], impacts: [AVAudioPlayer] = []
    private var chime: AVAudioPlayer?
    private var lastClick = -Double.infinity
    init() {
        for variant in 0..<3 {
            if let p = try? AVAudioPlayer(data: Self.wave(kind: 0, variant: variant)) { p.prepareToPlay(); clicks.append(p) }
            for _ in 0..<3 {
                if let p = try? AVAudioPlayer(data: Self.wave(kind: 1, variant: variant)) { p.prepareToPlay(); launches.append(p) }
                if let url = Bundle.main.url(forResource: "impact-soft-\(variant)", withExtension: "wav"), let p = try? AVAudioPlayer(contentsOf: url) { p.prepareToPlay(); impacts.append(p) }
            }
        }
        chime = try? AVAudioPlayer(data: Self.wave(kind: 2, variant: 0)); chime?.prepareToPlay()
    }
    var ready: Bool { clicks.count == 3 && launches.count == 9 && impacts.count == 9 && chime != nil }
    func detent(sound: Bool, haptics: Bool) {
        let now = ProcessInfo.processInfo.systemUptime
        guard now - lastClick >= 0.05 else { return }; lastClick = now
        if sound { play(clicks, gain: 0.65, pan: 0) }
        if haptics { NSHapticFeedbackManager.defaultPerformer.perform(.alignment, performanceTime: .now) }
    }
    func launch(pan: Double) { play(launches, gain: 0.24, pan: pan) }
    func impact(speed: Double, size: Double, settling: Bool, pan: Double) {
        let gain = (0.22 + 0.25 * min(1, max(0.2, speed / 1050))) * min(1, max(0.5, size / 100)) * (settling ? 0.35 : 1)
        play(impacts, gain: gain, pan: pan)
    }
    func complete() { chime?.currentTime = 0; chime?.volume = 0.35; chime?.play() }
    func stop() { (clicks + launches + impacts + [chime].compactMap { $0 }).forEach { $0.stop(); $0.currentTime = 0 } }
    private func play(_ pool: [AVAudioPlayer], gain: Double, pan: Double) {
        guard let player = pool.first(where: { !$0.isPlaying }) else { return }
        player.currentTime = 0; player.volume = Float(gain); player.pan = Float(max(-1, min(1, pan))); player.play()
    }
    static func wave(kind: Int, variant: Int) -> Data {
        let rate = 44100, duration = kind == 0 ? 0.04 : kind == 1 ? 0.075 + Double(variant) * 0.005 : 0.65
        let count = Int(Double(rate) * duration)
        var data = Data()
        func string(_ s: String) { data.append(contentsOf: s.utf8) }
        func u16(_ n: UInt16) { data.append(UInt8(n & 255)); data.append(UInt8(n >> 8)) }
        func u32(_ n: Int) { u16(UInt16(n & 65535)); u16(UInt16(n >> 16)) }
        string("RIFF"); u32(36 + count * 2); string("WAVEfmt "); u32(16); u16(1); u16(1)
        u32(rate); u32(rate * 2); u16(2); u16(16); string("data"); u32(count * 2)
        var seed = UInt64(1700 + variant), low = 0.0, medium = 0.0, previous = 0.0, highpass = 0.0
        for i in 0..<count {
            seed = seed &* 6364136223846793005 &+ 1
            let noise = Double(seed >> 33) / Double(UInt32.max >> 1) * 2 - 1
            let t = Double(i) / Double(rate), sample: Double
            if kind == 0 {
                low += 0.34 * (noise - low)
                let contact = (low - previous) * exp(-t / 0.0018); previous = low
                let tune = 1 + Double(variant - 1) * 0.018
                let body = 0.62 * sin(2 * .pi * tune * (510 * t - 2600 * t * t)) * exp(-t / 0.0048)
                let shell = 0.19 * sin(2 * .pi * 1120 * tune * t + 0.25) * exp(-t / 0.0024)
                let seat = t < 0.006 ? 0 : 0.14 * sin(2 * .pi * 760 * tune * (t - 0.006)) * exp(-(t - 0.006) / 0.002)
                sample = (body + shell + 0.48 * contact + seat) * min(1, t / 0.00023) * min(1, Double(count - 1 - i) / 176) * 0.24
            } else if kind == 1 {
                low += 0.085 * (noise - low); medium += 0.24 * (noise - medium)
                let air = ((medium - low) * 0.25 + low * 0.04) * pow(sin(.pi * t / duration), 2)
                highpass = 0.994 * (highpass + air - previous); previous = air
                sample = highpass * min(1, Double(count - 1 - i) / (Double(rate) * 0.012))
            } else {
                sample = (sin(2 * .pi * 660 * t) + 0.3 * sin(2 * .pi * 990 * t)) * exp(-t * 9) * min(1, t / 0.012) * 0.3 * min(1, (duration - t) / 0.05)
            }
            u16(UInt16(bitPattern: Int16(max(-1, min(1, sample)) * 32767)))
        }
        return data
    }
}
