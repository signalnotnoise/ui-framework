# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,953.39 | 4,112.27 | 4.02% | within budget |
| full-list | Milliseconds | 4,217.31 | 3,289.20 | -22.01% | within budget |
| full-list | MountAllocatedBytes | 336,437,144.00 | 333,017,464.00 | -1.02% | within budget |
| full-list | AllocatedBytes | 623,462,160.00 | 466,614,032.00 | -25.16% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 382.32 | 384.62 | 0.60% | within budget |
| virtualized | Milliseconds | 828.11 | 783.81 | -5.35% | within budget |
| virtualized | MountAllocatedBytes | 9,679,928.00 | 9,531,512.00 | -1.53% | within budget |
| virtualized | AllocatedBytes | 63,211,216.00 | 59,920,272.00 | -5.21% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,682.37 | 3,702.80 | 0.55% | within budget |
| themed-full-list | Milliseconds | 7,663.16 | 3,594.35 | -53.10% | within budget |
| themed-full-list | MountAllocatedBytes | 327,693,704.00 | 328,357,512.00 | 0.20% | within budget |
| themed-full-list | AllocatedBytes | 1,072,720,080.00 | 472,009,208.00 | -56.00% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,058.00 | 2,055.20 | -0.14% | within budget |
| layout-editors | Milliseconds | 8,905.20 | 8,448.94 | -5.12% | within budget |
| layout-editors | MountAllocatedBytes | 126,193,944.00 | 121,501,184.00 | -3.72% | within budget |
| layout-editors | AllocatedBytes | 1,161,579,664.00 | 1,123,291,632.00 | -3.30% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
