# Paired rendering performance

Baseline: dc570c709807cecacd0ed0ef341fc0c6aef615e7. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 3,816.90 | 3,935.52 | 3.11% | within budget |
| full-list | Milliseconds | 3,210.04 | 3,370.23 | 4.99% | within budget |
| full-list | MountAllocatedBytes | 332,907,112.00 | 333,228,400.00 | 0.10% | within budget |
| full-list | AllocatedBytes | 466,487,000.00 | 468,058,216.00 | 0.34% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 409.88 | 446.38 | 8.90% | within budget |
| virtualized | Milliseconds | 443.18 | 422.52 | -4.66% | within budget |
| virtualized | MountAllocatedBytes | 9,539,024.00 | 9,567,096.00 | 0.29% | within budget |
| virtualized | AllocatedBytes | 47,885,296.00 | 48,389,864.00 | 1.05% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 165.00 | 165.00 | 0.00% | within budget |
| virtualized | Mounts | 69.00 | 69.00 | 0.00% | within budget |
| virtualized | Unmounts | 66.00 | 66.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 3,954.96 | 4,055.52 | 2.54% | within budget |
| themed-full-list | Milliseconds | 4,021.16 | 4,032.07 | 0.27% | within budget |
| themed-full-list | MountAllocatedBytes | 328,253,192.00 | 328,182,648.00 | -0.02% | within budget |
| themed-full-list | AllocatedBytes | 471,119,016.00 | 472,626,504.00 | 0.32% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
