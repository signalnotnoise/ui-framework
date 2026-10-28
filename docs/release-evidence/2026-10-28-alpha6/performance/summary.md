# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,584.23 | 3,585.32 | 0.03% | within budget |
| full-list | Milliseconds | 4,683.71 | 3,369.64 | -28.06% | within budget |
| full-list | MountAllocatedBytes | 336,459,976.00 | 332,862,856.00 | -1.07% | within budget |
| full-list | AllocatedBytes | 623,729,240.00 | 465,983,016.00 | -25.29% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 288.65 | 316.42 | 9.62% | within budget |
| virtualized | Milliseconds | 796.15 | 830.11 | 4.26% | within budget |
| virtualized | MountAllocatedBytes | 9,664,304.00 | 9,508,608.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 62,857,536.00 | 59,883,808.00 | -4.73% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,377.89 | 3,550.20 | 5.10% | within budget |
| themed-full-list | Milliseconds | 8,406.63 | 3,596.71 | -57.22% | within budget |
| themed-full-list | MountAllocatedBytes | 327,536,552.00 | 327,867,736.00 | 0.10% | within budget |
| themed-full-list | AllocatedBytes | 1,074,661,984.00 | 471,145,216.00 | -56.16% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,590.00 | 1,575.66 | -0.90% | within budget |
| layout-editors | Milliseconds | 8,995.40 | 8,818.02 | -1.97% | within budget |
| layout-editors | MountAllocatedBytes | 126,177,616.00 | 121,487,032.00 | -3.72% | within budget |
| layout-editors | AllocatedBytes | 1,161,698,176.00 | 1,123,283,416.00 | -3.31% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
