# Paired rendering performance

Baseline: 78c88fb901c202e3c2e49b6de300d1ce2369e00b. Samples per side: 7. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 4,744.25 | 4,851.51 | 2.26% | within budget |
| full-list | Milliseconds | 5,634.20 | 4,026.51 | -28.53% | within budget |
| full-list | MountAllocatedBytes | 348,298,392.00 | 344,605,656.00 | -1.06% | within budget |
| full-list | AllocatedBytes | 642,999,152.00 | 474,455,136.00 | -26.21% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 321.70 | 329.75 | 2.50% | within budget |
| virtualized | Milliseconds | 1,016.10 | 1,026.66 | 1.04% | within budget |
| virtualized | MountAllocatedBytes | 9,757,016.00 | 9,599,760.00 | -1.61% | within budget |
| virtualized | AllocatedBytes | 64,348,672.00 | 61,064,512.00 | -5.10% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 4,764.44 | 4,719.97 | -0.93% | within budget |
| themed-full-list | Milliseconds | 10,706.37 | 4,579.12 | -57.23% | within budget |
| themed-full-list | MountAllocatedBytes | 339,468,080.00 | 330,154,088.00 | -2.74% | within budget |
| themed-full-list | AllocatedBytes | 1,093,446,264.00 | 468,481,088.00 | -57.16% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,327.76 | 2,313.00 | -0.63% | within budget |
| layout-editors | Milliseconds | 11,765.28 | 11,280.07 | -4.12% | within budget |
| layout-editors | MountAllocatedBytes | 126,191,896.00 | 126,310,224.00 | 0.09% | within budget |
| layout-editors | AllocatedBytes | 1,161,271,136.00 | 1,123,090,456.00 | -3.29% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
