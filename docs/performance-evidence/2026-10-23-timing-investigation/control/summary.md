# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 8,330.50 | 8,815.27 | 5.82% | within budget |
| full-list | Milliseconds | 9,153.99 | 8,895.03 | -2.83% | within budget |
| full-list | MountAllocatedBytes | 342,702,424.00 | 342,437,376.00 | -0.08% | within budget |
| full-list | AllocatedBytes | 464,347,568.00 | 464,727,112.00 | 0.08% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 418.42 | 437.33 | 4.52% | within budget |
| virtualized | Milliseconds | 1,115.55 | 1,205.47 | 8.06% | within budget |
| virtualized | MountAllocatedBytes | 9,620,304.00 | 9,620,304.00 | 0.00% | within budget |
| virtualized | AllocatedBytes | 61,100,144.00 | 61,100,616.00 | 0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 8,070.10 | 7,747.13 | -4.00% | within budget |
| themed-full-list | Milliseconds | 11,253.29 | 9,736.18 | -13.48% | within budget |
| themed-full-list | MountAllocatedBytes | 327,881,808.00 | 327,923,472.00 | 0.01% | within budget |
| themed-full-list | AllocatedBytes | 458,568,480.00 | 461,183,904.00 | 0.57% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,922.40 | 1,920.43 | -0.10% | within budget |
| layout-editors | Milliseconds | 21,452.95 | 21,699.64 | 1.15% | within budget |
| layout-editors | MountAllocatedBytes | 126,360,512.00 | 126,359,608.00 | -0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,009,080.00 | 1,113,029,016.00 | -0.89% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
