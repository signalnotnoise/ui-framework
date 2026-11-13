# FW-010 final interactive evidence — November 11, 2026

Archive contains the 3/3 pilot, 9/9 repeated interactive cases, process logs, app-cropped
screenshots, phase markers, full Release regression log/TRX/stress/startup/native-window
reports, and a tested-source snapshot checked against the repeated source manifest.
Every zip entry was re-read and SHA-256 checked against manifest.json. Profiles,
caches, dumps and binaries are excluded. Runtime 157.0.4341.42, SDK 1.0.4314.35,
.NET 10.0.13; base64bd242 plus uncommitted acceptance/sample/docs changes.

The user authorized these test-window interactions. The computer-use tool supplied
pointer clicks/scrolls using fresh test-window screenshots; the sample verified
trusted HTML events, focus, scroll and pixels. An initial pilot accessibility-index
click could not resolve geometry; screenshot-coordinate input succeeded. A refresh
after successful scrolling found the completed test window already closed. No
application test failed or assertions/timeouts changed to obtain these passes.

This is local interactive acceptance, not unattended CI or a performance release
gate. See [scope and results](../../webview2-final-2026-11-11.md).
