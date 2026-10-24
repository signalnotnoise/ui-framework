# Paired rendering performance

Baseline: fbf5204051fa15da420a4917c2b1fe15fc746a78. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 6,155.41 | 5,556.60 | -9.73% | within budget |
| full-list | Milliseconds | 4,843.01 | 6,939.44 | 43.29% | REGRESSION |
| full-list | MountAllocatedBytes | 342,703,888.00 | 342,738,720.00 | 0.01% | within budget |
| full-list | AllocatedBytes | 470,209,392.00 | 470,133,424.00 | -0.02% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 429.61 | 399.21 | -7.08% | within budget |
| virtualized | Milliseconds | 1,079.92 | 1,088.45 | 0.79% | within budget |
| virtualized | MountAllocatedBytes | 9,620,464.00 | 9,620,304.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,097,232.00 | 61,100,464.00 | 0.01% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 7,917.24 | 8,010.43 | 1.18% | within budget |
| themed-full-list | Milliseconds | 14,485.36 | 15,280.81 | 5.49% | within budget |
| themed-full-list | MountAllocatedBytes | 328,083,144.00 | 327,819,072.00 | -0.08% | within budget |
| themed-full-list | AllocatedBytes | 458,569,304.00 | 458,810,872.00 | 0.05% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,892.45 | 1,908.38 | 0.84% | within budget |
| layout-editors | Milliseconds | 18,884.38 | 19,481.82 | 3.16% | within budget |
| layout-editors | MountAllocatedBytes | 126,362,848.00 | 126,363,384.00 | 0.00% | within budget |
| layout-editors | AllocatedBytes | 1,122,998,392.00 | 1,123,008,032.00 | 0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
| native-inherit | MountMilliseconds | 330.73 | 330.10 | -0.19% | within budget |
| native-inherit | Milliseconds | 924.88 | 939.51 | 1.58% | within budget |
| native-inherit | MountAllocatedBytes | 18,778,136.00 | 18,778,096.00 | -0.00% | within budget |
| native-inherit | AllocatedBytes | 110,650,536.00 | 110,650,536.00 | 0.00% | within budget |
| native-inherit | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| native-inherit | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| native-inherit | Mounts | 80.00 | 80.00 | 0.00% | within budget |
| native-inherit | Unmounts | 80.00 | 80.00 | 0.00% | within budget |
| native-preserve | MountMilliseconds | 308.63 | 319.99 | 3.68% | within budget |
| native-preserve | Milliseconds | 547.80 | 554.38 | 1.20% | within budget |
| native-preserve | MountAllocatedBytes | 16,475,408.00 | 16,475,280.00 | -0.00% | within budget |
| native-preserve | AllocatedBytes | 88,120,088.00 | 88,116,664.00 | -0.00% | within budget |
| native-preserve | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| native-preserve | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| native-preserve | Mounts | 80.00 | 80.00 | 0.00% | within budget |
| native-preserve | Unmounts | 80.00 | 80.00 | 0.00% | within budget |
