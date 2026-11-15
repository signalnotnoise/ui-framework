# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 4,508.83 | 4,492.70 | -0.36% | within budget |
| full-list | Milliseconds | 5,191.35 | 3,755.06 | -27.67% | within budget |
| full-list | MountAllocatedBytes | 336,139,200.00 | 332,728,000.00 | -1.01% | within budget |
| full-list | AllocatedBytes | 622,528,296.00 | 465,690,584.00 | -25.19% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 329.72 | 336.54 | 2.07% | within budget |
| virtualized | Milliseconds | 934.99 | 491.97 | -47.38% | within budget |
| virtualized | MountAllocatedBytes | 9,676,672.00 | 9,515,544.00 | -1.67% | within budget |
| virtualized | AllocatedBytes | 63,165,376.00 | 47,849,048.00 | -24.25% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 165.00 | -14.95% | within budget |
| virtualized | Mounts | 98.00 | 69.00 | -29.59% | within budget |
| virtualized | Unmounts | 95.00 | 66.00 | -30.53% | within budget |
| themed-full-list | MountMilliseconds | 4,541.74 | 4,668.89 | 2.80% | within budget |
| themed-full-list | Milliseconds | 9,557.47 | 4,361.98 | -54.36% | within budget |
| themed-full-list | MountAllocatedBytes | 327,380,576.00 | 327,454,952.00 | 0.02% | within budget |
| themed-full-list | AllocatedBytes | 1,069,210,152.00 | 470,283,200.00 | -56.02% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,189.08 | 2,178.14 | -0.50% | within budget |
| layout-editors | Milliseconds | 10,924.05 | 10,498.04 | -3.90% | within budget |
| layout-editors | MountAllocatedBytes | 125,472,976.00 | 120,772,880.00 | -3.75% | within budget |
| layout-editors | AllocatedBytes | 1,161,283,600.00 | 1,123,092,488.00 | -3.29% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
