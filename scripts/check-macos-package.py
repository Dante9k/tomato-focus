"""Check the exact signed application payload; no developer helpers are distributed."""
import plistlib
import sys
from pathlib import Path

app = Path(sys.argv[1])
version = sys.argv[2]
expected = {
    "Contents/Info.plist", "Contents/MacOS/TomatoFocus",
    "Contents/_CodeSignature/CodeResources", "Contents/Resources/tomato-cute.png",
    "Contents/Resources/TomatoFocus.icns", "Contents/Resources/Kenney-License.txt",
    *(f"Contents/Resources/impact-soft-{i}.wav" for i in range(3)),
}
actual = {p.relative_to(app).as_posix() for p in app.rglob("*") if p.is_file()}
if actual != expected:
    raise SystemExit(f"Unexpected app payload. Missing: {expected - actual}; extra: {actual - expected}")
if any(p.is_symlink() for p in app.rglob("*")):
    raise SystemExit("Application payload must not contain symlinks")
with (app / "Contents/Info.plist").open("rb") as stream:
    info = plistlib.load(stream)
assert info["CFBundleShortVersionString"] == version
assert info["CFBundleIdentifier"] == "com.dante9k.tomatofocus"
assert info["LSUIElement"] is True
assert info["LSMinimumSystemVersion"] == "13.0"
assert (app / "Contents/MacOS/TomatoFocus").stat().st_size > 100_000
print("PASS: macOS application allowlist, version, architecture metadata and bundled resources")
