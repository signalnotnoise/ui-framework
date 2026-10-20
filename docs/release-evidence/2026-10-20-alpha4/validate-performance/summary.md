# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,586.87 | 3,767.81 | 5.04% | within budget |
| full-list | Milliseconds | 5,131.81 | 3,541.09 | -31.00% | within budget |
| full-list | MountAllocatedBytes | 348,400,744.00 | 345,086,848.00 | -0.95% | within budget |
| full-list | AllocatedBytes | 645,468,872.00 | 475,279,928.00 | -26.37% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 277.37 | 284.31 | 2.50% | within budget |
| virtualized | Milliseconds | 834.88 | 837.52 | 0.32% | within budget |
| virtualized | MountAllocatedBytes | 9,757,016.00 | 9,599,760.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,283,328.00 | 61,066,600.00 | -5.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,755.23 | 3,574.29 | -4.82% | within budget |
| themed-full-list | Milliseconds | 9,155.41 | 3,730.45 | -59.25% | within budget |
| themed-full-list | MountAllocatedBytes | 339,576,112.00 | 330,007,568.00 | -2.82% | within budget |
| themed-full-list | AllocatedBytes | 1,097,458,160.00 | 468,692,192.00 | -57.29% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,571.35 | 1,611.91 | 2.58% | within budget |
| layout-editors | Milliseconds | 9,934.59 | 9,291.80 | -6.47% | within budget |
| layout-editors | MountAllocatedBytes | 126,194,144.00 | 126,356,200.00 | 0.13% | within budget |
| layout-editors | AllocatedBytes | 1,161,534,896.00 | 1,123,148,880.00 | -3.30% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
