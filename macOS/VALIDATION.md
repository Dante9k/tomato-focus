# macOS validation

The Mac edition shares the existing tomato artwork, vector numeral paths, detent synthesis design, landing samples, and shake thresholds with the Windows edition. The operating-system integration is a separate Swift implementation.

## Automated coverage

- Domain tests: deadline persistence and sleep/expiry math; backward-compatible settings defaults; duration limits; horizontal/vertical shake, small jitter, slow direction changes, ordinary drag; wheel wrapping, inertia and reduced-motion settling; ballistic landing, one bounce, two impact events and bounded lifetime.
- Native UI verification (isolated state): silent presets, wheel-to-duration event, preloaded audio, 250 → 125-point transition, fixed upper-right anchor, fruit opacity, hidden editor, hide/show while counting, actual elapsed deadline, reminder at the same position, dismissal, cancel/restore and persisted state. PNG captures exercise the actual AppKit view.
- Package: both Mach-O architectures, macOS deployment target in Info.plist, strict ad-hoc signature verification, exact resource file allowlist, DMG/ZIP and SHA-256.

## Physical Mac checks still required

- Force Touch feedback, mouse drag/reversal comfort and trackpad inertia feel.
- Perceived detent/throw/landing sound quality through speakers and headphones.
- Retina/external/mixed-scale monitors; menu bar on multiple screens; Spaces, Stage Manager and full-screen applications.
- Sustained frame pacing at different refresh rates and with GPU load. The animation caps particles at 32, caps catch-up time, uses fixed physics substeps, preloads resources, and drops occupied audio voices instead of queueing; these bounds are not a measured FPS guarantee.
- Downloaded-app first launch through Gatekeeper. The current package is not Developer ID signed or notarized; ad-hoc signature validation is not notarization.

The projectile overlay covers the widget's current display, not every attached display. Reduce Motion uses a static reminder. System haptics availability is not reported as physically verified by CI.
