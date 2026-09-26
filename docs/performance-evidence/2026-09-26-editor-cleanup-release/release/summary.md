# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,225.77 | 5,252.17 | 0.51% | within budget |
| full-list | Milliseconds | 6,265.46 | 4,168.29 | -33.47% | within budget |
| full-list | MountAllocatedBytes | 346,439,864.00 | 342,926,624.00 | -1.01% | within budget |
| full-list | AllocatedBytes | 636,599,448.00 | 471,294,904.00 | -25.97% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 356.08 | 359.87 | 1.07% | within budget |
| virtualized | Milliseconds | 1,106.33 | 1,105.91 | -0.04% | within budget |
| virtualized | MountAllocatedBytes | 9,777,992.00 | 9,620,296.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,395,096.00 | 61,100,552.00 | -5.12% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,372.28 | 5,224.34 | -2.75% | within budget |
| themed-full-list | Milliseconds | 11,920.70 | 4,758.34 | -60.08% | within budget |
| themed-full-list | MountAllocatedBytes | 337,136,640.00 | 327,979,960.00 | -2.72% | within budget |
| themed-full-list | AllocatedBytes | 1,088,890,624.00 | 467,150,048.00 | -57.10% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,955.75 | 1,992.71 | 1.89% | within budget |
| layout-editors | Milliseconds | 9,124.90 | 8,935.45 | -2.08% | within budget |
| layout-editors | MountAllocatedBytes | 126,196,352.00 | 126,362,072.00 | 0.13% | within budget |
| layout-editors | AllocatedBytes | 1,161,528,104.00 | 1,123,256,368.00 | -3.29% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
