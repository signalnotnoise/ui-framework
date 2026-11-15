# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,841.21 | 3,997.45 | 4.07% | within budget |
| full-list | Milliseconds | 4,198.21 | 3,363.06 | -19.89% | within budget |
| full-list | MountAllocatedBytes | 336,483,912.00 | 333,181,744.00 | -0.98% | within budget |
| full-list | AllocatedBytes | 623,704,792.00 | 466,687,752.00 | -25.17% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 464.04 | 465.09 | 0.23% | within budget |
| virtualized | Milliseconds | 860.75 | 410.28 | -52.33% | within budget |
| virtualized | MountAllocatedBytes | 9,679,864.00 | 9,538,416.00 | -1.46% | within budget |
| virtualized | AllocatedBytes | 63,212,968.00 | 47,886,456.00 | -24.25% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 165.00 | -14.95% | within budget |
| virtualized | Mounts | 98.00 | 69.00 | -29.59% | within budget |
| virtualized | Unmounts | 95.00 | 66.00 | -30.53% | within budget |
| themed-full-list | MountMilliseconds | 3,604.48 | 3,670.04 | 1.82% | within budget |
| themed-full-list | Milliseconds | 7,276.64 | 3,617.02 | -50.29% | within budget |
| themed-full-list | MountAllocatedBytes | 327,718,952.00 | 328,241,240.00 | 0.16% | within budget |
| themed-full-list | AllocatedBytes | 1,072,554,144.00 | 471,341,744.00 | -56.05% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,878.77 | 1,969.27 | 4.82% | within budget |
| layout-editors | Milliseconds | 8,576.88 | 8,689.24 | 1.31% | within budget |
| layout-editors | MountAllocatedBytes | 125,471,656.00 | 120,772,952.00 | -3.74% | within budget |
| layout-editors | AllocatedBytes | 1,161,500,952.00 | 1,123,245,520.00 | -3.29% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
