# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,672.30 | 5,703.22 | 0.55% | within budget |
| full-list | Milliseconds | 6,043.27 | 4,651.27 | -23.03% | within budget |
| full-list | MountAllocatedBytes | 336,003,688.00 | 332,686,048.00 | -0.99% | within budget |
| full-list | AllocatedBytes | 621,692,152.00 | 464,831,856.00 | -25.23% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 426.40 | 426.82 | 0.10% | within budget |
| virtualized | Milliseconds | 1,367.95 | 635.45 | -53.55% | within budget |
| virtualized | MountAllocatedBytes | 9,664,304.00 | 9,527,880.00 | -1.41% | within budget |
| virtualized | AllocatedBytes | 63,165,376.00 | 47,849,048.00 | -24.25% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 165.00 | -14.95% | within budget |
| virtualized | Mounts | 98.00 | 69.00 | -29.59% | within budget |
| virtualized | Unmounts | 95.00 | 66.00 | -30.53% | within budget |
| themed-full-list | MountMilliseconds | 5,452.20 | 5,631.88 | 3.30% | within budget |
| themed-full-list | Milliseconds | 11,316.60 | 5,220.42 | -53.87% | within budget |
| themed-full-list | MountAllocatedBytes | 327,378,904.00 | 327,284,280.00 | -0.03% | within budget |
| themed-full-list | AllocatedBytes | 1,067,825,544.00 | 469,245,584.00 | -56.06% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,821.27 | 2,819.94 | -0.05% | within budget |
| layout-editors | Milliseconds | 13,803.32 | 13,494.06 | -2.24% | within budget |
| layout-editors | MountAllocatedBytes | 125,465,328.00 | 120,755,288.00 | -3.75% | within budget |
| layout-editors | AllocatedBytes | 1,161,181,408.00 | 1,123,048,856.00 | -3.28% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
