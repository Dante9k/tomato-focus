import AppKit

enum WidgetLayout {
    static let editingSize: CGFloat = 220
    static let focusSize: CGFloat = 125
    static let reminderSize: CGFloat = 250
    static let canvasSize: CGFloat = 250
    // Existing settings store the origin of a 250-point square. Retaining it preserves the
    // upper-right anchor on upgrades, relaunches and rollback to the previous Mac version.
    static let savedCoordinateSize: CGFloat = 250
}
