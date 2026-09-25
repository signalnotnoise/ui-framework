# Paired rendering performance

Baseline: c25c521f243cfa646223694486cb46b98e31cf2f. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 16,821.32 | 18,020.57 | 7.13% | within budget |
| full-list | Milliseconds | 30,278.27 | 32,765.89 | 8.22% | within budget |
| full-list | MountAllocatedBytes | 341,783,560.00 | 342,162,272.00 | 0.11% | within budget |
| full-list | AllocatedBytes | 598,204,384.00 | 598,934,640.00 | 0.12% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 543.92 | 527.85 | -2.95% | within budget |
| virtualized | Milliseconds | 1,677.72 | 1,696.09 | 1.09% | within budget |
| virtualized | MountAllocatedBytes | 9,620,424.00 | 9,620,168.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,098,760.00 | 61,103,136.00 | 0.01% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 17,873.85 | 14,817.90 | -17.10% | within budget |
| themed-full-list | Milliseconds | 63,868.76 | 63,498.90 | -0.58% | within budget |
| themed-full-list | MountAllocatedBytes | 326,509,080.00 | 326,799,640.00 | 0.09% | within budget |
| themed-full-list | AllocatedBytes | 1,161,743,216.00 | 1,157,102,688.00 | -0.40% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
