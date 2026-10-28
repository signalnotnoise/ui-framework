# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,782.02 | 3,894.40 | 2.97% | within budget |
| full-list | Milliseconds | 3,974.25 | 3,232.14 | -18.67% | within budget |
| full-list | MountAllocatedBytes | 336,377,560.00 | 332,981,872.00 | -1.01% | within budget |
| full-list | AllocatedBytes | 623,365,920.00 | 467,218,104.00 | -25.05% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 435.31 | 453.68 | 4.22% | within budget |
| virtualized | Milliseconds | 950.69 | 888.89 | -6.50% | within budget |
| virtualized | MountAllocatedBytes | 9,679,864.00 | 9,531,512.00 | -1.53% | within budget |
| virtualized | AllocatedBytes | 63,211,312.00 | 59,921,640.00 | -5.20% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,925.45 | 3,891.75 | -0.86% | within budget |
| themed-full-list | Milliseconds | 7,704.35 | 3,703.01 | -51.94% | within budget |
| themed-full-list | MountAllocatedBytes | 327,667,256.00 | 344,005,536.00 | 4.99% | REGRESSION |
| themed-full-list | AllocatedBytes | 1,072,527,056.00 | 471,270,000.00 | -56.06% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,557.47 | 2,045.92 | -20.00% | within budget |
| layout-editors | Milliseconds | 8,481.90 | 8,091.24 | -4.61% | within budget |
| layout-editors | MountAllocatedBytes | 126,205,384.00 | 128,974,624.00 | 2.19% | REGRESSION |
| layout-editors | AllocatedBytes | 1,161,593,304.00 | 1,123,297,960.00 | -3.30% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
