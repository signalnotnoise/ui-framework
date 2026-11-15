# FW-017 appearance evidence

[Acceptance record](../../fw017-appearance-acceptance-2026-11-15.md).

Eight PNGs are actual WPF renders at 96 DPI. Names identify palette, width and
normal/focus template state. Every capture was individually inspected; focus is
injected on a hidden HWND and does not claim physical keyboard interaction.

- [Light, wide](light-1040-normal.png) / [focused](light-1040-focus.png)
- [Dark, wide](dark-1040-normal.png) / [focused](dark-1040-focus.png)
- [Light, compact](light-760-normal.png) / [focused](light-760-focus.png)
- [Dark, compact](dark-760-normal.png) / [focused](dark-760-focus.png)

`report.json` records 222 repeated assertions and capture metadata;
`tests/appearance.trx` records 44 passing focused tests. Logs include the initial
fixture-only compile failure and the successful correction. `fixture-sources.zip`
contains reproducible fixture source; `source-manifest.json` records fixture and
framework hashes. `runtime-provenance.json` verifies 54 core/WPF entries against
the prior memory-budget final-source manifest. No runtime changes or fresh
performance/release gate are claimed. `manifest.json` hashes every evidence file
other than this explanatory README and the manifest itself.
