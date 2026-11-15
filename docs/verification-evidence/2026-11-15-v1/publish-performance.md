# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 6,052.66 | 6,003.61 | -0.81% | within budget |
| full-list | Milliseconds | 6,290.20 | 4,739.79 | -24.65% | within budget |
| full-list | MountAllocatedBytes | 336,042,056.00 | 332,618,648.00 | -1.02% | within budget |
| full-list | AllocatedBytes | 619,745,704.00 | 464,616,600.00 | -25.03% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 425.70 | 432.01 | 1.48% | within budget |
| virtualized | Milliseconds | 1,316.61 | 700.21 | -46.82% | within budget |
| virtualized | MountAllocatedBytes | 9,664,336.00 | 9,515,544.00 | -1.54% | within budget |
| virtualized | AllocatedBytes | 63,165,376.00 | 47,849,048.00 | -24.25% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 165.00 | -14.95% | within budget |
| virtualized | Mounts | 98.00 | 69.00 | -29.59% | within budget |
| virtualized | Unmounts | 95.00 | 66.00 | -30.53% | within budget |
| themed-full-list | MountMilliseconds | 5,927.72 | 6,137.04 | 3.53% | within budget |
| themed-full-list | Milliseconds | 11,888.31 | 5,399.27 | -54.58% | within budget |
| themed-full-list | MountAllocatedBytes | 327,410,360.00 | 327,396,048.00 | -0.00% | within budget |
| themed-full-list | AllocatedBytes | 1,064,393,032.00 | 469,534,760.00 | -55.89% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 3,081.99 | 3,151.81 | 2.27% | within budget |
| layout-editors | Milliseconds | 15,561.53 | 15,182.58 | -2.44% | within budget |
| layout-editors | MountAllocatedBytes | 125,461,600.00 | 120,712,592.00 | -3.79% | within budget |
| layout-editors | AllocatedBytes | 1,156,407,376.00 | 1,112,692,280.00 | -3.78% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
