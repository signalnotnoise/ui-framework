# Paired rendering performance

Baseline: 62db456c950f737268f554a0439496d0fdb617a6. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,896.40 | 3,878.34 | -0.46% | within budget |
| full-list | Milliseconds | 3,437.53 | 3,332.58 | -3.05% | within budget |
| full-list | MountAllocatedBytes | 333,007,784.00 | 333,027,920.00 | 0.01% | within budget |
| full-list | AllocatedBytes | 466,517,888.00 | 466,899,448.00 | 0.08% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 541.67 | 424.09 | -21.71% | within budget |
| virtualized | Milliseconds | 867.03 | 884.91 | 2.06% | within budget |
| virtualized | MountAllocatedBytes | 9,531,056.00 | 9,530,552.00 | -0.01% | within budget |
| virtualized | AllocatedBytes | 59,902,336.00 | 59,896,288.00 | -0.01% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,801.98 | 3,847.62 | 1.20% | within budget |
| themed-full-list | Milliseconds | 4,029.98 | 4,210.04 | 4.47% | within budget |
| themed-full-list | MountAllocatedBytes | 328,341,216.00 | 328,310,160.00 | -0.01% | within budget |
| themed-full-list | AllocatedBytes | 470,580,968.00 | 470,618,864.00 | 0.01% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,925.22 | 1,979.49 | 2.82% | within budget |
| layout-editors | Milliseconds | 8,315.20 | 8,333.11 | 0.22% | within budget |
| layout-editors | MountAllocatedBytes | 121,504,600.00 | 121,503,936.00 | -0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,321,816.00 | 1,123,356,912.00 | 0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
