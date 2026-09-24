import Foundation

public struct TimerState: Codable, Equatable {
    public var duration = 25 * 60
    public var deadline: Date?
    public var x: Double?
    public var y: Double?
    public var wheelSound = true
    public var completionSound = true
    public var effectsSound = true
    public var haptics = true
    public init() {}

    public func remaining(at now: Date = Date()) -> Int {
        guard let deadline else { return duration }
        return max(0, Int(ceil(deadline.timeIntervalSince(now))))
    }

    public mutating func start(at now: Date = Date()) {
        duration = min(86399, max(1, duration))
        deadline = now.addingTimeInterval(Double(duration))
    }

    public mutating func cancel() { deadline = nil }

    // Missing fields keep their defaults so later versions can extend this file safely.
    enum CodingKeys: String, CodingKey {
        case duration, deadline, x, y, wheelSound, completionSound, effectsSound, haptics
    }
    public init(from decoder: Decoder) throws {
        self.init()
        let c = try decoder.container(keyedBy: CodingKeys.self)
        duration = min(86399, max(1, try c.decodeIfPresent(Int.self, forKey: .duration) ?? duration))
        deadline = try c.decodeIfPresent(Date.self, forKey: .deadline)
        x = try c.decodeIfPresent(Double.self, forKey: .x)
        y = try c.decodeIfPresent(Double.self, forKey: .y)
        wheelSound = try c.decodeIfPresent(Bool.self, forKey: .wheelSound) ?? wheelSound
        completionSound = try c.decodeIfPresent(Bool.self, forKey: .completionSound) ?? completionSound
        effectsSound = try c.decodeIfPresent(Bool.self, forKey: .effectsSound) ?? effectsSound
        haptics = try c.decodeIfPresent(Bool.self, forKey: .haptics) ?? haptics
    }
}

public final class StateStore {
    public let url: URL
    public init(url: URL) { self.url = url }
    public func load() throws -> TimerState {
        guard FileManager.default.fileExists(atPath: url.path) else { return TimerState() }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode(TimerState.self, from: Data(contentsOf: url))
    }
    public func save(_ state: TimerState) throws {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
        try encoder.encode(state).write(to: url, options: .atomic)
    }
}
