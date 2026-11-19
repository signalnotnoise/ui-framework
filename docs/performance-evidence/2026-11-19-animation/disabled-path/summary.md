# Paired rendering performance

Baseline: dc570c709807cecacd0ed0ef341fc0c6aef615e7. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,230.78 | 6,253.82 | 19.56% | REGRESSION |
| full-list | Milliseconds | 7,225.72 | 7,369.91 | 2.00% | within budget |
| full-list | MountAllocatedBytes | 332,967,448.00 | 333,394,184.00 | 0.13% | within budget |
| full-list | AllocatedBytes | 463,224,984.00 | 466,161,904.00 | 0.63% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 427.11 | 411.85 | -3.57% | within budget |
| virtualized | Milliseconds | 419.44 | 421.29 | 0.44% | within budget |
| virtualized | MountAllocatedBytes | 9,538,992.00 | 9,568,824.00 | 0.31% | within budget |
| virtualized | AllocatedBytes | 47,885,168.00 | 48,390,792.00 | 1.06% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 165.00 | 165.00 | 0.00% | within budget |
| virtualized | Mounts | 69.00 | 69.00 | 0.00% | within budget |
| virtualized | Unmounts | 66.00 | 66.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 6,188.49 | 6,013.59 | -2.83% | within budget |
| themed-full-list | Milliseconds | 8,490.07 | 8,422.30 | -0.80% | within budget |
| themed-full-list | MountAllocatedBytes | 328,131,520.00 | 328,809,184.00 | 0.21% | within budget |
| themed-full-list | AllocatedBytes | 466,253,000.00 | 470,600,344.00 | 0.93% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
