#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
VERSION="$(tr -d '\r\n' < VERSION)"
BUILD="$ROOT/build/macos"
DIST="$ROOT/dist"
APP="$BUILD/Tomato Focus.app"
CONTENTS="$APP/Contents"
mkdir -p "$BUILD" "$DIST"
swift test --package-path macOS --scratch-path "$BUILD/tests"
for ARCH in arm64 x86_64; do
    swift build --package-path macOS --configuration release --arch "$ARCH" --scratch-path "$BUILD/$ARCH"
    BIN="$(swift build --package-path macOS --configuration release --arch "$ARCH" --scratch-path "$BUILD/$ARCH" --show-bin-path)"
    cp "$BIN/TomatoFocus" "$BUILD/TomatoFocus-$ARCH"
done
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"
lipo -create "$BUILD/TomatoFocus-arm64" "$BUILD/TomatoFocus-x86_64" -output "$CONTENTS/MacOS/TomatoFocus"
cp assets/tomato-cute.png "$CONTENTS/Resources/"
for N in 0 1 2; do cp "assets/audio/impact-soft-$N.wav" "$CONTENTS/Resources/"; done
cp assets/audio/source/Kenney-License.txt "$CONTENTS/Resources/"
ICONSET="$BUILD/TomatoFocus.iconset"
mkdir -p "$ICONSET"
for SIZE in 16 32 128 256 512; do
    sips -z "$SIZE" "$SIZE" assets/brand/tomato-focus.png --out "$ICONSET/icon_${SIZE}x${SIZE}.png" >/dev/null
    DOUBLE=$((SIZE * 2))
    sips -z "$DOUBLE" "$DOUBLE" assets/brand/tomato-focus.png --out "$ICONSET/icon_${SIZE}x${SIZE}@2x.png" >/dev/null
done
iconutil --convert icns "$ICONSET" --output "$CONTENTS/Resources/TomatoFocus.icns"
cat > "$CONTENTS/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>TomatoFocus</string>
<key>CFBundleIdentifier</key><string>com.dante9k.tomatofocus</string>
<key>CFBundleName</key><string>Tomato Focus</string>
<key>CFBundleDisplayName</key><string>朱果 · Tomato Focus</string>
<key>CFBundleShortVersionString</key><string>$VERSION</string>
<key>CFBundleVersion</key><string>$VERSION</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleIconFile</key><string>TomatoFocus</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>LSUIElement</key><true/>
<key>NSHighResolutionCapable</key><true/>
<key>NSPrincipalClass</key><string>NSApplication</string>
<key>NSHumanReadableCopyright</key><string>Copyright © Dante9k. All rights reserved.</string>
</dict></plist>
PLIST
plutil -lint "$CONTENTS/Info.plist"
codesign --force --sign - --timestamp=none "$APP"
codesign --verify --deep --strict --verbose=2 "$APP"
lipo "$CONTENTS/MacOS/TomatoFocus" -verify_arch arm64 x86_64
"$CONTENTS/MacOS/TomatoFocus" --verify-ui "$BUILD/verification"
# Rebuilding must not silently ship unexpected resources from an older build.
python3 scripts/check-macos-package.py "$APP" "$VERSION"
NAME="TomatoFocus-$VERSION-macos-universal"
STAGE="$BUILD/$NAME"
mkdir -p "$STAGE"
# The DMG-only Applications link from a preceding build is not part of the ZIP.
if [ -L "$STAGE/Applications" ]; then unlink "$STAGE/Applications"; fi
ditto "$APP" "$STAGE/Tomato Focus.app"
cp macOS/README.md "$STAGE/README.md"
cp macOS/README.zh-CN.md "$STAGE/README.zh-CN.md"
cp macOS/VALIDATION.md "$STAGE/VALIDATION.md"
cp LICENSE "$STAGE/LICENSE"
python3 scripts/check-macos-package.py "$STAGE/Tomato Focus.app" "$VERSION" --container
ditto -c -k --sequesterRsrc --keepParent "$STAGE" "$DIST/$NAME.zip"
ln -sfn /Applications "$STAGE/Applications"
hdiutil create -volname "Tomato Focus" -srcfolder "$STAGE" -ov -format UDZO "$DIST/$NAME.dmg"
hdiutil verify "$DIST/$NAME.dmg"
MOUNT="$BUILD/mounted"
mkdir -p "$MOUNT"
hdiutil attach "$DIST/$NAME.dmg" -readonly -nobrowse -mountpoint "$MOUNT"
trap 'hdiutil detach "$MOUNT" >/dev/null || true' EXIT
python3 scripts/check-macos-package.py "$MOUNT/Tomato Focus.app" "$VERSION" --container
codesign --verify --deep --strict "$MOUNT/Tomato Focus.app"
hdiutil detach "$MOUNT"
trap - EXIT
for EXT in zip dmg; do
    (cd "$DIST" && shasum -a 256 "$NAME.$EXT" > "$NAME.$EXT.sha256")
done
echo "Built and verified $NAME (macOS 13+, Apple Silicon and Intel)."
