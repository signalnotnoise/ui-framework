# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 6,466.28 | 6,392.41 | -1.14% | within budget |
| full-list | Milliseconds | 6,971.57 | 5,081.22 | -27.12% | within budget |
| full-list | MountAllocatedBytes | 348,055,400.00 | 344,428,408.00 | -1.04% | within budget |
| full-list | AllocatedBytes | 639,800,200.00 | 473,222,712.00 | -26.04% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 411.44 | 419.51 | 1.96% | within budget |
| virtualized | Milliseconds | 1,461.21 | 1,381.96 | -5.42% | within budget |
| virtualized | MountAllocatedBytes | 9,757,032.00 | 9,599,760.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,348,672.00 | 61,065,736.00 | -5.10% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 6,592.73 | 6,261.93 | -5.02% | within budget |
| themed-full-list | Milliseconds | 13,718.64 | 5,670.45 | -58.67% | within budget |
| themed-full-list | MountAllocatedBytes | 337,701,776.00 | 329,497,544.00 | -2.43% | within budget |
| themed-full-list | AllocatedBytes | 1,089,599,384.00 | 467,647,416.00 | -57.08% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 3,315.94 | 3,271.19 | -1.35% | within budget |
| layout-editors | Milliseconds | 16,929.87 | 16,530.59 | -2.36% | within budget |
| layout-editors | MountAllocatedBytes | 126,182,952.00 | 126,314,952.00 | 0.10% | within budget |
| layout-editors | AllocatedBytes | 1,156,378,064.00 | 1,112,653,720.00 | -3.78% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
