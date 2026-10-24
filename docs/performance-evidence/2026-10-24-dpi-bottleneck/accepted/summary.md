# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,965.28 | 4,046.93 | 2.06% | within budget |
| full-list | Milliseconds | 4,597.22 | 3,591.69 | -21.87% | within budget |
| full-list | MountAllocatedBytes | 336,382,896.00 | 333,054,760.00 | -0.99% | within budget |
| full-list | AllocatedBytes | 623,699,368.00 | 466,464,568.00 | -25.21% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 403.85 | 403.17 | -0.17% | within budget |
| virtualized | Milliseconds | 834.83 | 817.46 | -2.08% | within budget |
| virtualized | MountAllocatedBytes | 9,679,744.00 | 9,529,736.00 | -1.55% | within budget |
| virtualized | AllocatedBytes | 62,981,528.00 | 59,917,824.00 | -4.86% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,555.15 | 3,529.69 | -0.72% | within budget |
| themed-full-list | Milliseconds | 7,626.89 | 3,457.12 | -54.67% | within budget |
| themed-full-list | MountAllocatedBytes | 327,627,504.00 | 318,164,920.00 | -2.89% | within budget |
| themed-full-list | AllocatedBytes | 1,072,238,808.00 | 460,657,080.00 | -57.04% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,911.84 | 1,954.21 | 2.22% | within budget |
| layout-editors | Milliseconds | 8,554.32 | 8,101.96 | -5.29% | within budget |
| layout-editors | MountAllocatedBytes | 126,199,144.00 | 126,367,720.00 | 0.13% | within budget |
| layout-editors | AllocatedBytes | 1,161,540,568.00 | 1,123,267,976.00 | -3.29% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
