# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,078.51 | 5,109.98 | 0.62% | within budget |
| full-list | Milliseconds | 5,994.61 | 3,879.18 | -35.29% | within budget |
| full-list | MountAllocatedBytes | 346,508,208.00 | 342,939,200.00 | -1.03% | within budget |
| full-list | AllocatedBytes | 636,645,968.00 | 473,696,416.00 | -25.60% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 364.22 | 376.38 | 3.34% | within budget |
| virtualized | Milliseconds | 1,129.74 | 1,120.16 | -0.85% | within budget |
| virtualized | MountAllocatedBytes | 9,777,152.00 | 9,620,168.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,365,880.00 | 61,070,072.00 | -5.12% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,180.12 | 4,963.15 | -4.19% | within budget |
| themed-full-list | Milliseconds | 11,134.23 | 4,270.61 | -61.64% | within budget |
| themed-full-list | MountAllocatedBytes | 337,585,640.00 | 328,282,968.00 | -2.76% | within budget |
| themed-full-list | AllocatedBytes | 1,088,814,576.00 | 467,542,120.00 | -57.06% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,381.25 | 2,330.33 | -2.14% | within budget |
| layout-editors | Milliseconds | 8,528.02 | 8,057.29 | -5.52% | within budget |
| layout-editors | MountAllocatedBytes | 126,205,104.00 | 126,364,464.00 | 0.13% | within budget |
| layout-editors | AllocatedBytes | 1,161,571,784.00 | 1,123,240,648.00 | -3.30% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
