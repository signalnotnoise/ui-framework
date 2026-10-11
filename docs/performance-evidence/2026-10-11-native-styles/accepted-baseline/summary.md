# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,165.30 | 5,198.29 | 0.64% | within budget |
| full-list | Milliseconds | 6,468.85 | 4,114.68 | -36.39% | within budget |
| full-list | MountAllocatedBytes | 346,467,880.00 | 342,971,544.00 | -1.01% | within budget |
| full-list | AllocatedBytes | 636,640,416.00 | 473,897,568.00 | -25.56% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 348.01 | 360.84 | 3.69% | within budget |
| virtualized | Milliseconds | 1,155.34 | 1,154.07 | -0.11% | within budget |
| virtualized | MountAllocatedBytes | 9,777,992.00 | 9,620,296.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,393,728.00 | 61,102,056.00 | -5.11% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,138.48 | 4,992.03 | -2.85% | within budget |
| themed-full-list | Milliseconds | 11,150.35 | 4,585.68 | -58.87% | within budget |
| themed-full-list | MountAllocatedBytes | 337,336,912.00 | 328,157,424.00 | -2.72% | within budget |
| themed-full-list | AllocatedBytes | 1,089,139,528.00 | 467,448,104.00 | -57.08% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,999.74 | 2,003.73 | 0.20% | within budget |
| layout-editors | Milliseconds | 8,756.20 | 8,326.39 | -4.91% | within budget |
| layout-editors | MountAllocatedBytes | 126,200,120.00 | 126,364,616.00 | 0.13% | within budget |
| layout-editors | AllocatedBytes | 1,161,537,968.00 | 1,123,293,208.00 | -3.29% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
