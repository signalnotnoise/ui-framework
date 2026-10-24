# Experimental presentation cache priming

Both sides execute identical candidate payloads; only candidate primes an empty presentation source. Archived setup revision 78c88fb901c202e3c2e49b6de300d1ce2369e00b is not executed. Samples per side: 7. Experimental, never release eligible; editor workload already owns a presentation source.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 4,999.33 | 3,675.23 | -26.49% | within budget |
| full-list | Milliseconds | 3,911.54 | 3,030.58 | -22.52% | within budget |
| full-list | MountAllocatedBytes | 342,857,616.00 | 333,093,640.00 | -2.85% | within budget |
| full-list | AllocatedBytes | 472,092,952.00 | 467,168,992.00 | -1.04% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 431.57 | 409.78 | -5.05% | within budget |
| virtualized | Milliseconds | 1,125.53 | 805.83 | -28.40% | within budget |
| virtualized | MountAllocatedBytes | 9,632,688.00 | 9,529,736.00 | -1.07% | within budget |
| virtualized | AllocatedBytes | 61,103,232.00 | 59,695,864.00 | -2.30% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 4,737.16 | 3,515.60 | -25.79% | within budget |
| themed-full-list | Milliseconds | 4,225.27 | 3,205.66 | -24.13% | within budget |
| themed-full-list | MountAllocatedBytes | 327,980,168.00 | 318,148,880.00 | -3.00% | within budget |
| themed-full-list | AllocatedBytes | 467,607,840.00 | 460,441,992.00 | -1.53% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 1,909.74 | 1,934.79 | 1.31% | within budget |
| layout-editors | Milliseconds | 7,732.95 | 7,879.30 | 1.89% | within budget |
| layout-editors | MountAllocatedBytes | 126,314,384.00 | 126,362,192.00 | 0.04% | within budget |
| layout-editors | AllocatedBytes | 1,123,244,792.00 | 1,123,298,520.00 | 0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
