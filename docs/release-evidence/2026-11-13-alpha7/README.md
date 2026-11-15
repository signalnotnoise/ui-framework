# Alpha.7 release evidence

Candidate source: `c5d639bcff707ff1a75b23f2a60135d6be6cb7b2`.
See the [release record](../../releases/0.1.0-alpha.7.md) for interpretation,
publication status and acceptance limits.

- `local-release.log`: build, regression, visual, stress and lifecycle validation.
- `local-packages.log`: isolated local package consumer checks.
- `local-performance/`: seven alternating pairs plus warmups for full-list,
  virtualized, themed-full-list and layout-editors; summary, source/binary
  manifests, process metadata and raw runs.
- `local-source-verification.json`: all 194 captured files match the committed
  candidate after CRLF/LF normalization; raw manifest hashes match the snapshot.
- `hosted-performance/`: independent Validate workflow comparison on the exact
  candidate, with all raw samples and unchanged accepted budgets.
- `validate-run.json` and `publish-run.json`: exact source SHA and completed job/step results.
- `release-performance/`: the publishing workflow's independent seven-pair comparison.
- `publication.log`: NuGet acceptance of both libraries and both symbol packages.
- `indexing-observation.json`: initial not-yet-indexed responses after upload.
- `public-installation-attempt-1.log`: expected NU1102 before indexing completed.
- `public-installation.log` and `.json`: successful fresh public restore/run and
  both installed assembly hashes matching the publication artifacts.
- `manifest.json`: SHA-256 fingerprints of the preserved evidence files.

Negative metric deltas mean improvements. Mount and update times, UI-thread
allocations and component work are all retained. These are fixed-workload
measurements, not application-wide guarantees. A local dirty flag is preserved
rather than rewritten; concurrent FW-018 changes did not alter captured sources.
