# Paired rendering performance

Baseline: c39d4ae665db7f4c23a9283e02e0b476d874d17e. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,326.24 | 5,200.28 | -2.36% | within budget |
| full-list | Milliseconds | 5,933.38 | 3,987.63 | -32.79% | within budget |
| full-list | MountAllocatedBytes | 342,976,560.00 | 342,918,672.00 | -0.02% | within budget |
| full-list | AllocatedBytes | 604,987,024.00 | 473,376,672.00 | -21.75% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 365.33 | 366.08 | 0.21% | within budget |
| virtualized | Milliseconds | 1,020.47 | 1,090.17 | 6.83% | within budget |
| virtualized | MountAllocatedBytes | 9,619,568.00 | 9,619,296.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 60,470,608.00 | 61,067,896.00 | 0.99% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 4,877.73 | 4,869.83 | -0.16% | within budget |
| themed-full-list | Milliseconds | 11,644.15 | 4,127.64 | -64.55% | within budget |
| themed-full-list | MountAllocatedBytes | 328,438,400.00 | 328,180,880.00 | -0.08% | within budget |
| themed-full-list | AllocatedBytes | 1,159,151,720.00 | 467,938,016.00 | -59.63% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,294.38 | 2,317.03 | 0.99% | within budget |
| layout-editors | Milliseconds | 7,811.14 | 8,104.01 | 3.75% | within budget |
| layout-editors | MountAllocatedBytes | 126,362,696.00 | 126,365,776.00 | 0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,249,768.00 | 1,123,338,912.00 | 0.01% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
