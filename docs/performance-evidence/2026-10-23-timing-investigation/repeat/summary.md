# Paired rendering performance

Baseline: 13a5b98cad533bd9b33dcbd5b5f234d03033985c. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 8,323.06 | 8,815.31 | 5.91% | within budget |
| full-list | Milliseconds | 10,840.88 | 11,128.96 | 2.66% | within budget |
| full-list | MountAllocatedBytes | 342,732,712.00 | 342,784,712.00 | 0.02% | within budget |
| full-list | AllocatedBytes | 462,302,880.00 | 463,892,368.00 | 0.34% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 406.88 | 413.17 | 1.55% | within budget |
| virtualized | Milliseconds | 1,143.34 | 1,174.29 | 2.71% | within budget |
| virtualized | MountAllocatedBytes | 9,620,464.00 | 9,620,304.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,101,544.00 | 61,102,432.00 | 0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 7,389.95 | 7,291.67 | -1.33% | within budget |
| themed-full-list | Milliseconds | 9,796.38 | 9,632.56 | -1.67% | within budget |
| themed-full-list | MountAllocatedBytes | 327,792,144.00 | 327,852,936.00 | 0.02% | within budget |
| themed-full-list | AllocatedBytes | 459,212,896.00 | 459,269,472.00 | 0.01% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,059.53 | 2,031.11 | -1.38% | within budget |
| layout-editors | Milliseconds | 19,113.28 | 19,022.66 | -0.47% | within budget |
| layout-editors | MountAllocatedBytes | 126,365,760.00 | 126,367,032.00 | 0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,003,976.00 | 1,123,003,288.00 | -0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
