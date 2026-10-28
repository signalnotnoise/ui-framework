# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 4,087.21 | 4,072.68 | -0.36% | within budget |
| full-list | Milliseconds | 4,252.85 | 3,326.59 | -21.78% | within budget |
| full-list | MountAllocatedBytes | 336,486,256.00 | 333,056,952.00 | -1.02% | within budget |
| full-list | AllocatedBytes | 623,320,032.00 | 466,953,232.00 | -25.09% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 430.77 | 398.52 | -7.49% | within budget |
| virtualized | Milliseconds | 824.82 | 849.82 | 3.03% | within budget |
| virtualized | MountAllocatedBytes | 9,679,880.00 | 9,531,464.00 | -1.53% | within budget |
| virtualized | AllocatedBytes | 63,211,088.00 | 59,920,184.00 | -5.21% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 4,345.39 | 4,296.31 | -1.13% | within budget |
| themed-full-list | Milliseconds | 8,703.01 | 4,034.08 | -53.65% | within budget |
| themed-full-list | MountAllocatedBytes | 327,735,368.00 | 356,053,936.00 | 8.64% | REGRESSION |
| themed-full-list | AllocatedBytes | 1,072,710,968.00 | 486,154,504.00 | -54.68% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,152.08 | 2,397.58 | 11.41% | REGRESSION |
| layout-editors | Milliseconds | 8,654.04 | 9,067.34 | 4.78% | within budget |
| layout-editors | MountAllocatedBytes | 126,206,912.00 | 134,886,120.00 | 6.88% | REGRESSION |
| layout-editors | AllocatedBytes | 1,161,526,720.00 | 1,134,271,592.00 | -2.35% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
