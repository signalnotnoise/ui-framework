# Paired rendering performance

Baseline: c39d4ae665db7f4c23a9283e02e0b476d874d17e. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,153.57 | 5,108.87 | -0.87% | within budget |
| full-list | Milliseconds | 6,012.74 | 5,940.78 | -1.20% | within budget |
| full-list | MountAllocatedBytes | 342,884,584.00 | 343,006,424.00 | 0.04% | within budget |
| full-list | AllocatedBytes | 606,175,664.00 | 606,194,688.00 | 0.00% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 369.16 | 382.37 | 3.58% | within budget |
| virtualized | Milliseconds | 1,110.01 | 1,024.57 | -7.70% | within budget |
| virtualized | MountAllocatedBytes | 9,620,456.00 | 9,620,168.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,100,504.00 | 60,498,048.00 | -0.99% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 4,890.95 | 5,297.60 | 8.31% | within budget |
| themed-full-list | Milliseconds | 11,298.35 | 11,246.84 | -0.46% | within budget |
| themed-full-list | MountAllocatedBytes | 328,288,696.00 | 329,578,256.00 | 0.39% | within budget |
| themed-full-list | AllocatedBytes | 1,159,075,096.00 | 1,164,660,312.00 | 0.48% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
