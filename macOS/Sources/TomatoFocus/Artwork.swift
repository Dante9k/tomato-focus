import AppKit

enum Artwork {
    static let tomato: NSImage = {
        guard let url = Bundle.main.url(forResource: "tomato-cute", withExtension: "png"),
              let image = NSImage(contentsOf: url) else { fatalError("Missing tomato artwork in application bundle") }
        return image
    }()
    static let cream = NSColor(calibratedRed: 0.97, green: 0.95, blue: 0.91, alpha: 1)
    static let coral = NSColor(calibratedRed: 0.98, green: 0.36, blue: 0.30, alpha: 1)
    static var reducedMotion: Bool { NSWorkspace.shared.accessibilityDisplayShouldReduceMotion }

    static func text(_ value: String, rect: NSRect, size: CGFloat, color: NSColor,
                     weight: NSFont.Weight = .medium, centered: Bool = true) {
        let style = NSMutableParagraphStyle(); style.alignment = centered ? .center : .left
        (value as NSString).draw(in: rect, withAttributes: [
            .font: NSFont.monospacedDigitSystemFont(ofSize: size, weight: weight),
            .foregroundColor: color, .paragraphStyle: style
        ])
    }

    // These are the same original rounded, broad-stroke apertures as the Windows app.
    static let numerals: [CGPath] = [
        "M10,2 C4,2 2,7 2,15 C2,23 4,28 10,28 C16,28 18,23 18,15 C18,7 16,2 10,2 Z",
        "M5,8 Q9,5 12,2 L12,28",
        "M2,7 C4,0 16,0 18,7 C20,15 7,17 3,25 Q1,28 5,28 L18,28",
        "M3,4 C9,0 18,2 18,8 C18,13 13,15 9,15 M9,15 C14,15 19,18 18,23 C17,29 7,30 2,25",
        "M13,2 L3,18 Q1,21 5,21 L19,21 M15,12 L15,28",
        "M18,2 L4,2 L3,14 C7,10 18,12 18,20 C18,28 8,31 2,25",
        "M16,3 C7,-1 2,10 2,19 C2,32 19,30 18,20 C17,12 4,11 2,19",
        "M2,2 L17,2 Q20,2 17,7 C12,15 8,22 7,28",
        "M10,2 C-1,2 0,14 10,15 C20,14 21,2 10,2 Z M10,15 C-2,16 0,28 10,28 C20,28 22,16 10,15 Z",
        "M18,12 C18,-1 1,0 2,10 C3,18 16,19 18,11 M18,11 C18,20 13,31 4,27"
    ].map { source in
        let pattern = try! NSRegularExpression(pattern: "[MLCQZ]|-?[0-9]+(?:\\.[0-9]+)?")
        let string = source as NSString
        let tokens = pattern.matches(in: source, range: NSRange(location: 0, length: string.length)).map { string.substring(with: $0.range) }
        let path = CGMutablePath(); var i = 0
        func point() -> CGPoint { let x = Double(tokens[i])!, y = Double(tokens[i + 1])!; i += 2; return CGPoint(x: x, y: y) }
        while i < tokens.count {
            let command = tokens[i]; i += 1
            switch command {
            case "M": path.move(to: point())
            case "L": path.addLine(to: point())
            case "C": let a = point(), b = point(), c = point(); path.addCurve(to: c, control1: a, control2: b)
            case "Q": let a = point(), b = point(); path.addQuadCurve(to: b, control: a)
            default: path.closeSubpath()
            }
        }
        return path.copy(strokingWithWidth: 4.8, lineCap: .round, lineJoin: .round, miterLimit: 10)
    }

    static func readout(_ seconds: Int, in context: CGContext) {
        let string = seconds >= 3600
            ? String(format: "%02d:%02d:%02d", seconds / 3600, seconds / 60 % 60, seconds % 60)
            : String(format: "%02d:%02d", seconds / 60, seconds % 60)
        let scale: CGFloat = seconds >= 3600 ? 0.75 : 1
        let width = CGFloat(string.filter { $0 != ":" }.count) * 24 + CGFloat(string.filter { $0 == ":" }.count) * 8
        var x = (250 - width * scale) / 2
        let aperture = CGMutablePath()
        for character in string {
            if character == ":" {
                for y: CGFloat in [9, 22] { aperture.addEllipse(in: CGRect(x: x + 1.7 * scale, y: 136 + (y * 0.86 - 2.3) * scale, width: 4.6 * scale, height: 4.6 * scale)) }
                x += 8 * scale
            } else if let number = character.wholeNumberValue {
                let transform = CGAffineTransform(a: scale, b: 0, c: 0, d: scale * 0.86, tx: x + 2 * scale, ty: 136)
                aperture.addPath(numerals[number], transform: transform)
                x += 24 * scale
            }
        }
        context.saveGState()
        context.addPath(aperture)
        context.setStrokeColor(NSColor(calibratedRed: 0.4, green: 0.15, blue: 0.12, alpha: 0.35).cgColor)
        context.setLineWidth(1.2); context.strokePath()
        context.addPath(aperture); context.clip()
        let colors = [NSColor(white: 0.80, alpha: 1).cgColor, NSColor(white: 0.97, alpha: 1).cgColor, NSColor(white: 0.87, alpha: 1).cgColor] as CFArray
        let gradient = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(), colors: colors, locations: [0, 0.45, 1])!
        context.drawLinearGradient(gradient, start: CGPoint(x: 0, y: 136), end: CGPoint(x: 0, y: 163), options: [.drawsBeforeStartLocation, .drawsAfterEndLocation])
        context.translateBy(x: 0.35, y: 0.65)
        context.addPath(aperture); context.setStrokeColor(NSColor(calibratedRed: 0.4, green: 0.18, blue: 0.14, alpha: 0.36).cgColor)
        context.setLineWidth(0.9); context.strokePath()
        context.restoreGState()
    }
}

func localized(_ english: String, _ chinese: String) -> String {
    Locale.preferredLanguages.first?.hasPrefix("zh") == true ? chinese : english
}
