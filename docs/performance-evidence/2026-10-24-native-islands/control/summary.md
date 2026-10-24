# Identical binary measurement control

Both sides execute the candidate payload; archived setup revision 78c88fb901c202e3c2e49b6de300d1ce2369e00b is not executed. Samples per side: 7. Differences measure variation, not an implementation improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 9,058.07 | 7,982.95 | -11.87% | within budget |
| full-list | Milliseconds | 13,289.82 | 9,780.64 | -26.41% | within budget |
| full-list | MountAllocatedBytes | 342,638,384.00 | 342,764,848.00 | 0.04% | within budget |
| full-list | AllocatedBytes | 463,854,248.00 | 464,331,552.00 | 0.10% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
