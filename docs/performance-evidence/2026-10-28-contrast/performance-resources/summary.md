# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,927.37 | 4,030.62 | 2.63% | within budget |
| full-list | Milliseconds | 4,205.87 | 3,381.52 | -19.60% | within budget |
| full-list | MountAllocatedBytes | 336,476,024.00 | 333,007,752.00 | -1.03% | within budget |
| full-list | AllocatedBytes | 623,317,704.00 | 467,021,720.00 | -25.07% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 396.31 | 396.47 | 0.04% | within budget |
| virtualized | Milliseconds | 816.82 | 834.11 | 2.12% | within budget |
| virtualized | MountAllocatedBytes | 9,679,040.00 | 9,530,592.00 | -1.53% | within budget |
| virtualized | AllocatedBytes | 63,181,184.00 | 59,894,136.00 | -5.20% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,596.84 | 4,205.34 | 16.92% | REGRESSION |
| themed-full-list | Milliseconds | 8,109.39 | 3,524.80 | -56.53% | within budget |
| themed-full-list | MountAllocatedBytes | 327,661,848.00 | 357,560,320.00 | 9.12% | REGRESSION |
| themed-full-list | AllocatedBytes | 1,072,437,088.00 | 460,354,592.00 | -57.07% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,198.45 | 2,681.41 | 21.97% | REGRESSION |
| layout-editors | Milliseconds | 8,318.32 | 7,888.26 | -5.17% | within budget |
| layout-editors | MountAllocatedBytes | 126,192,320.00 | 159,816,840.00 | 26.65% | REGRESSION |
| layout-editors | AllocatedBytes | 1,161,637,496.00 | 1,123,342,208.00 | -3.30% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
