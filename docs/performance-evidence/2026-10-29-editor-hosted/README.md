# FW-022 hosted validation — October 29, 2026

[Validate run 40867541344](https://github.com/signalnotnoise/ui-framework/actions/runs/40867541344)
passed for exact clean commit `f7c7320dfa118839efc41f1b8477c4f08ac5ad9a`.
All three jobs passed: Windows correctness/API/packages, graph tooling, performance.

- Full seven-pair comparison passes **32/32**, performance `releaseEligible: true`.
- Themed mount: 5760.05 to 5896.04 ms, **+2.36%**.
- Editor mount: 3069.25 to 3148.89 ms, **+2.59%**.
- Editor updates: **-1.05%**; mount/update allocation **-3.76%/-3.78%**.
- All 16 raw editor reports (warmups included) confirm source-before-editors-v1,
  source-first initialization and phase tracing disabled.
- Windows passes 159 tests, 15 visual checks, 21 stress assertions, startup,
  API guard and package checks. Graph tooling/outputs pass.

`performance-comparison.zip` is the original artifact 12895161911; its SHA-256
matches GitHub's published digest:
`253c73b09439d9cc860ab2b5f0a3c9be266575a8f5e260594fd025ed9372bc1a`.
`summary.json` is extracted without alteration. `run.json` records exact head and
job conclusions; `windows.log` preserves correctness/package output.
`manifest.json` hashes the preserved evidence files.

The accepted baseline remains `78c88fb`; budgets are unchanged. Hosted SDK
10.0.403/runtime 10.0.14 differs from local SDK 10.0.402/runtime 10.0.13, and
runner hardware/OS also differ. Passing here does not prove the cause of local
variance. The earlier local 30/32 campaign remains a failed diagnostic result.
Under the user-approved hosted-gate policy this exact implementation satisfies
the performance gate. Manual FW-005/FW-006 acceptance and any future release
candidate's own workflow requirements remain separate. This is not a publication.
