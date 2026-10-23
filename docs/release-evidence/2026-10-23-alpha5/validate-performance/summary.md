# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 6,074.49 | 6,060.10 | -0.24% | within budget |
| full-list | Milliseconds | 6,843.64 | 5,027.49 | -26.54% | within budget |
| full-list | MountAllocatedBytes | 347,841,160.00 | 344,534,168.00 | -0.95% | within budget |
| full-list | AllocatedBytes | 641,599,056.00 | 473,500,728.00 | -26.20% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 413.03 | 415.55 | 0.61% | within budget |
| virtualized | Milliseconds | 1,274.92 | 1,372.29 | 7.64% | within budget |
| virtualized | MountAllocatedBytes | 9,757,016.00 | 9,599,760.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,348,672.00 | 61,064,800.00 | -5.10% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 6,228.77 | 6,061.42 | -2.69% | within budget |
| themed-full-list | Milliseconds | 13,317.68 | 5,794.22 | -56.49% | within budget |
| themed-full-list | MountAllocatedBytes | 339,035,272.00 | 329,543,520.00 | -2.80% | within budget |
| themed-full-list | AllocatedBytes | 1,089,708,792.00 | 467,415,752.00 | -57.11% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 3,058.62 | 3,139.62 | 2.65% | within budget |
| layout-editors | Milliseconds | 15,217.86 | 14,410.26 | -5.31% | within budget |
| layout-editors | MountAllocatedBytes | 126,143,704.00 | 126,352,160.00 | 0.17% | within budget |
| layout-editors | AllocatedBytes | 1,161,114,328.00 | 1,123,044,448.00 | -3.28% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
