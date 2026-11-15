# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,911.45 | 6,000.21 | 1.50% | within budget |
| full-list | Milliseconds | 6,028.99 | 4,687.43 | -22.25% | within budget |
| full-list | MountAllocatedBytes | 336,191,832.00 | 332,701,040.00 | -1.04% | within budget |
| full-list | AllocatedBytes | 619,591,328.00 | 465,044,656.00 | -24.94% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 426.32 | 432.63 | 1.48% | within budget |
| virtualized | Milliseconds | 1,400.58 | 687.74 | -50.90% | within budget |
| virtualized | MountAllocatedBytes | 9,676,640.00 | 9,515,544.00 | -1.66% | within budget |
| virtualized | AllocatedBytes | 63,165,376.00 | 47,849,048.00 | -24.25% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 165.00 | -14.95% | within budget |
| virtualized | Mounts | 98.00 | 69.00 | -29.59% | within budget |
| virtualized | Unmounts | 95.00 | 66.00 | -30.53% | within budget |
| themed-full-list | MountMilliseconds | 5,797.10 | 5,990.19 | 3.33% | within budget |
| themed-full-list | Milliseconds | 11,825.91 | 5,394.48 | -54.38% | within budget |
| themed-full-list | MountAllocatedBytes | 327,414,512.00 | 327,356,824.00 | -0.02% | within budget |
| themed-full-list | AllocatedBytes | 1,064,404,512.00 | 469,301,072.00 | -55.91% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 3,123.51 | 3,132.06 | 0.27% | within budget |
| layout-editors | Milliseconds | 15,837.24 | 15,620.62 | -1.37% | within budget |
| layout-editors | MountAllocatedBytes | 125,468,904.00 | 120,771,080.00 | -3.74% | within budget |
| layout-editors | AllocatedBytes | 1,156,373,104.00 | 1,112,985,744.00 | -3.75% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
