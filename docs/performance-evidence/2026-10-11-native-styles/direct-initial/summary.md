# Paired rendering performance

Baseline: 533fbe178145aa7d9d51b7e8c2444c78399513b1. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,174.57 | 5,230.42 | 1.08% | within budget |
| full-list | Milliseconds | 4,192.16 | 4,485.87 | 7.01% | within budget |
| full-list | MountAllocatedBytes | 342,784,600.00 | 342,887,048.00 | 0.03% | within budget |
| full-list | AllocatedBytes | 471,343,080.00 | 474,084,728.00 | 0.58% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 372.21 | 388.11 | 4.27% | within budget |
| virtualized | Milliseconds | 1,135.42 | 1,235.90 | 8.85% | within budget |
| virtualized | MountAllocatedBytes | 9,620,616.00 | 9,620,296.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,099,896.00 | 61,098,320.00 | -0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,071.50 | 5,493.01 | 8.31% | within budget |
| themed-full-list | Milliseconds | 4,837.06 | 4,641.76 | -4.04% | within budget |
| themed-full-list | MountAllocatedBytes | 328,038,896.00 | 328,009,008.00 | -0.01% | within budget |
| themed-full-list | AllocatedBytes | 467,737,176.00 | 466,221,840.00 | -0.32% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,964.99 | 1,955.61 | -0.48% | within budget |
| layout-editors | Milliseconds | 8,357.66 | 8,465.72 | 1.29% | within budget |
| layout-editors | MountAllocatedBytes | 126,361,712.00 | 126,356,096.00 | -0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,244,072.00 | 1,123,252,096.00 | 0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
