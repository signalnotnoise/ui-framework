# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,375.76 | 5,676.62 | 5.60% | within budget |
| full-list | Milliseconds | 6,794.11 | 6,457.21 | -4.96% | within budget |
| full-list | MountAllocatedBytes | 346,436,408.00 | 342,958,496.00 | -1.00% | within budget |
| full-list | AllocatedBytes | 636,338,280.00 | 604,769,760.00 | -4.96% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 390.88 | 429.32 | 9.84% | within budget |
| virtualized | Milliseconds | 1,189.37 | 1,260.07 | 5.94% | within budget |
| virtualized | MountAllocatedBytes | 9,778,040.00 | 9,620,168.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,393,272.00 | 61,097,480.00 | -5.12% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 4,886.66 | 4,985.90 | 2.03% | within budget |
| themed-full-list | Milliseconds | 10,271.25 | 11,603.09 | 12.97% | REGRESSION |
| themed-full-list | MountAllocatedBytes | 337,451,216.00 | 328,436,136.00 | -2.67% | within budget |
| themed-full-list | AllocatedBytes | 1,089,112,480.00 | 1,159,002,104.00 | 6.42% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,223.91 | 2,235.86 | 0.54% | within budget |
| layout-editors | Milliseconds | 8,166.98 | 7,881.03 | -3.50% | within budget |
| layout-editors | MountAllocatedBytes | 126,187,600.00 | 126,355,792.00 | 0.13% | within budget |
| layout-editors | AllocatedBytes | 1,161,578,200.00 | 1,123,242,552.00 | -3.30% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
