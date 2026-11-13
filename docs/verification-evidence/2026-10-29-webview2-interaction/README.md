# WebView2 interaction pilot evidence — October 29, 2026

See [results, limits and foreground-use policy](../../webview2-interaction-2026-10-29.md).
`validation.zip` preserves the failed initial activation probe, the two passing
interaction cases, cropped test-window screenshots, source manifests and the
exact tested-source snapshot. Browser profile/cache contents are excluded.
`manifest.json` records each archived entry's length/SHA-256, verified after writing.

One focus/keyboard case and one airspace case passed. No three-run campaign or
hosted interaction gate is claimed. The subsequent opt-in safeguards compiled
without warnings/errors and the runner's missing-opt-in rejection path passed
without creating an output directory or opening a window. The source snapshot
predates those safeguards; the build log covers the final sample compilation.
The existing default lifetime cases still open their own nonactivated test windows;
the added interaction modes are excluded unless explicitly requested with the
interactive flag. No further GUI regression run was performed after the user
questioned desktop use.
