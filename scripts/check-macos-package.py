"""Check the exact signed application payload; no developer helpers are distributed."""
import plistlib
import sys
import os
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
if "--container" in sys.argv:
    allowed = {"Tomato Focus.app", "README.md", "README.zh-CN.md", "VALIDATION.md", "LICENSE"}
    files = {p.name for p in app.parent.iterdir()}
    if "Applications" in files:
        link = app.parent / "Applications"
        assert link.is_symlink() and os.readlink(link) == "/Applications"
        files.remove("Applications")
    # Finder may create a volume-local metadata directory when a DMG is mounted.
    files.discard(".fseventsd")
    assert files == allowed, f"Unexpected distribution content: {files ^ allowed}"
print("PASS: macOS application allowlist, version, architecture metadata and bundled resources")
