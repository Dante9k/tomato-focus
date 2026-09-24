// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "TomatoFocus",
    platforms: [.macOS(.v13)],
    products: [.executable(name: "TomatoFocus", targets: ["TomatoFocus"])],
    targets: [
        .target(name: "TomatoCore"),
        .executableTarget(name: "TomatoFocus", dependencies: ["TomatoCore"]),
        .testTarget(name: "TomatoCoreTests", dependencies: ["TomatoCore"])
    ]
)
