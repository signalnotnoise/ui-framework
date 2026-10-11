# Paired rendering performance

Baseline: 533fbe178145aa7d9d51b7e8c2444c78399513b1. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,367.51 | 5,214.60 | -2.85% | within budget |
| full-list | Milliseconds | 4,764.30 | 4,402.87 | -7.59% | within budget |
| full-list | MountAllocatedBytes | 342,839,936.00 | 342,849,360.00 | 0.00% | within budget |
| full-list | AllocatedBytes | 470,906,088.00 | 471,203,416.00 | 0.06% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 362.29 | 376.54 | 3.93% | within budget |
| virtualized | Milliseconds | 1,101.65 | 1,186.05 | 7.66% | within budget |
| virtualized | MountAllocatedBytes | 9,620,648.00 | 9,620,296.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,097,568.00 | 61,099,256.00 | 0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,497.36 | 5,724.27 | 4.13% | within budget |
| themed-full-list | Milliseconds | 4,437.72 | 4,728.19 | 6.55% | within budget |
| themed-full-list | MountAllocatedBytes | 328,113,224.00 | 327,859,952.00 | -0.08% | within budget |
| themed-full-list | AllocatedBytes | 467,495,376.00 | 467,354,232.00 | -0.03% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,088.21 | 2,009.07 | -3.79% | within budget |
| layout-editors | Milliseconds | 8,483.28 | 8,445.59 | -0.44% | within budget |
| layout-editors | MountAllocatedBytes | 126,365,504.00 | 126,364,656.00 | -0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,244,008.00 | 1,123,242,504.00 | -0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
