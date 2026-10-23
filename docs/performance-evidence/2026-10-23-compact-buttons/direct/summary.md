# Paired rendering performance

Baseline: 13a5b98cad533bd9b33dcbd5b5f234d03033985c. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 8,383.45 | 8,432.16 | 0.58% | within budget |
| full-list | Milliseconds | 13,773.09 | 17,748.26 | 28.86% | REGRESSION |
| full-list | MountAllocatedBytes | 342,856,696.00 | 342,732,880.00 | -0.04% | within budget |
| full-list | AllocatedBytes | 462,092,464.00 | 462,177,720.00 | 0.02% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 407.24 | 415.47 | 2.02% | within budget |
| virtualized | Milliseconds | 1,073.50 | 1,106.29 | 3.05% | within budget |
| virtualized | MountAllocatedBytes | 9,620,592.00 | 9,620,304.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,099,632.00 | 61,099,096.00 | -0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 7,523.99 | 7,361.90 | -2.15% | within budget |
| themed-full-list | Milliseconds | 14,192.36 | 14,564.92 | 2.63% | within budget |
| themed-full-list | MountAllocatedBytes | 328,038,320.00 | 328,074,840.00 | 0.01% | within budget |
| themed-full-list | AllocatedBytes | 459,251,688.00 | 459,213,776.00 | -0.01% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,007.14 | 1,959.50 | -2.37% | within budget |
| layout-editors | Milliseconds | 20,830.59 | 20,488.10 | -1.64% | within budget |
| layout-editors | MountAllocatedBytes | 126,364,680.00 | 126,363,640.00 | -0.00% | within budget |
| layout-editors | AllocatedBytes | 1,123,004,248.00 | 1,122,950,568.00 | -0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
