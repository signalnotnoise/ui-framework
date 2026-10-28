# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 6,209.67 | 6,261.04 | 0.83% | within budget |
| full-list | Milliseconds | 6,373.67 | 4,993.20 | -21.66% | within budget |
| full-list | MountAllocatedBytes | 336,187,608.00 | 332,616,736.00 | -1.06% | within budget |
| full-list | AllocatedBytes | 619,595,616.00 | 464,714,304.00 | -25.00% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 445.10 | 455.65 | 2.37% | within budget |
| virtualized | Milliseconds | 1,361.74 | 1,395.96 | 2.51% | within budget |
| virtualized | MountAllocatedBytes | 9,664,336.00 | 9,508,608.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 63,165,376.00 | 59,886,560.00 | -5.19% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,919.90 | 6,239.39 | 5.40% | within budget |
| themed-full-list | Milliseconds | 12,571.87 | 5,557.95 | -55.79% | within budget |
| themed-full-list | MountAllocatedBytes | 327,158,664.00 | 327,281,288.00 | 0.04% | within budget |
| themed-full-list | AllocatedBytes | 1,064,415,720.00 | 469,537,216.00 | -55.89% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 3,233.82 | 3,264.80 | 0.96% | within budget |
| layout-editors | Milliseconds | 15,971.87 | 15,676.93 | -1.85% | within budget |
| layout-editors | MountAllocatedBytes | 126,192,592.00 | 121,446,368.00 | -3.76% | within budget |
| layout-editors | AllocatedBytes | 1,156,368,856.00 | 1,113,028,968.00 | -3.75% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
