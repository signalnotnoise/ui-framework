# Paired rendering performance

Baseline: 13a5b98cad533bd9b33dcbd5b5f234d03033985c. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,150.44 | 5,163.53 | 0.25% | within budget |
| full-list | Milliseconds | 4,429.65 | 4,375.13 | -1.23% | within budget |
| full-list | MountAllocatedBytes | 342,615,312.00 | 342,372,632.00 | -0.07% | within budget |
| full-list | AllocatedBytes | 474,284,336.00 | 471,767,104.00 | -0.53% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 419.66 | 424.16 | 1.07% | within budget |
| virtualized | Milliseconds | 1,187.97 | 1,149.53 | -3.24% | within budget |
| virtualized | MountAllocatedBytes | 9,620,464.00 | 9,620,304.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,101,056.00 | 61,101,816.00 | 0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 5,651.75 | 7,148.48 | 26.48% | REGRESSION |
| themed-full-list | Milliseconds | 4,402.33 | 9,566.46 | 117.30% | REGRESSION |
| themed-full-list | MountAllocatedBytes | 328,146,976.00 | 327,812,400.00 | -0.10% | within budget |
| themed-full-list | AllocatedBytes | 466,885,160.00 | 459,625,512.00 | -1.55% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,160.45 | 2,125.99 | -1.60% | within budget |
| layout-editors | Milliseconds | 18,437.13 | 18,773.57 | 1.82% | within budget |
| layout-editors | MountAllocatedBytes | 126,366,680.00 | 126,374,520.00 | 0.01% | within budget |
| layout-editors | AllocatedBytes | 1,113,078,568.00 | 1,123,032,840.00 | 0.89% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
