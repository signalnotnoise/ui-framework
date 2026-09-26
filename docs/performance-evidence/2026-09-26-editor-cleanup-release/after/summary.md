# Paired rendering performance

Baseline: 2db7a9a6cdde0fa52c458a9206219348f4ae0bcd. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 7,411.69 | 5,295.43 | -28.55% | within budget |
| full-list | Milliseconds | 7,112.60 | 4,366.32 | -38.61% | within budget |
| full-list | MountAllocatedBytes | 342,816,368.00 | 342,682,096.00 | -0.04% | within budget |
| full-list | AllocatedBytes | 465,349,904.00 | 471,195,920.00 | 1.26% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 379.21 | 370.97 | -2.17% | within budget |
| virtualized | Milliseconds | 1,221.24 | 1,176.47 | -3.67% | within budget |
| virtualized | MountAllocatedBytes | 9,620,440.00 | 9,620,296.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,097,384.00 | 61,097,704.00 | 0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 7,732.32 | 7,549.27 | -2.37% | within budget |
| themed-full-list | Milliseconds | 9,767.33 | 6,342.41 | -35.07% | within budget |
| themed-full-list | MountAllocatedBytes | 327,650,088.00 | 327,722,936.00 | 0.02% | within budget |
| themed-full-list | AllocatedBytes | 459,396,056.00 | 463,706,192.00 | 0.94% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,228.13 | 2,207.28 | -0.94% | within budget |
| layout-editors | Milliseconds | 7,969.91 | 8,336.22 | 4.60% | within budget |
| layout-editors | MountAllocatedBytes | 126,361,976.00 | 126,366,408.00 | 0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,188,664.00 | 1,123,308,320.00 | 0.01% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
