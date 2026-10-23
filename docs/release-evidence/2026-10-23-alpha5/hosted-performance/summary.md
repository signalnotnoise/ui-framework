# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 6,300.74 | 6,319.34 | 0.30% | within budget |
| full-list | Milliseconds | 6,898.89 | 5,011.65 | -27.36% | within budget |
| full-list | MountAllocatedBytes | 347,898,560.00 | 344,456,272.00 | -0.99% | within budget |
| full-list | AllocatedBytes | 639,750,928.00 | 473,622,192.00 | -25.97% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 426.21 | 430.61 | 1.03% | within budget |
| virtualized | Milliseconds | 1,464.52 | 1,458.32 | -0.42% | within budget |
| virtualized | MountAllocatedBytes | 9,757,032.00 | 9,599,760.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,348,672.00 | 61,065,168.00 | -5.10% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 6,308.75 | 6,131.82 | -2.80% | within budget |
| themed-full-list | Milliseconds | 13,207.42 | 5,476.07 | -58.54% | within budget |
| themed-full-list | MountAllocatedBytes | 338,994,848.00 | 329,509,856.00 | -2.80% | within budget |
| themed-full-list | AllocatedBytes | 1,089,652,584.00 | 467,605,696.00 | -57.09% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 3,185.87 | 3,120.43 | -2.05% | within budget |
| layout-editors | Milliseconds | 15,454.99 | 15,121.10 | -2.16% | within budget |
| layout-editors | MountAllocatedBytes | 126,197,992.00 | 126,354,824.00 | 0.12% | within budget |
| layout-editors | AllocatedBytes | 1,156,371,416.00 | 1,112,645,336.00 | -3.78% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
