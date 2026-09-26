# Paired rendering performance

Baseline: 2db7a9a6cdde0fa52c458a9206219348f4ae0bcd. Samples per side: 3. Negative change is an improvement.

| Scenario | Metric | Baseline median | Candidate median | Change | Result |
| --- | --- | ---: | ---: | ---: | --- |
| full-list | MountMilliseconds | 5,227.95 | 5,259.79 | 0.61% | within budget |
| full-list | Milliseconds | 4,294.04 | 4,195.49 | -2.30% | within budget |
| full-list | MountAllocatedBytes | 342,766,432.00 | 342,998,528.00 | 0.07% | within budget |
| full-list | AllocatedBytes | 472,752,616.00 | 471,566,952.00 | -0.25% | within budget |
| full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| virtualized | MountMilliseconds | 376.67 | 390.84 | 3.76% | within budget |
| virtualized | Milliseconds | 1,244.94 | 1,198.08 | -3.76% | within budget |
| virtualized | MountAllocatedBytes | 9,620,440.00 | 9,620,168.00 | -0.00% | within budget |
| virtualized | AllocatedBytes | 61,100,344.00 | 61,097,528.00 | -0.00% | within budget |
| virtualized | MountBodyBuilds | 27.00 | 27.00 | 0.00% | within budget |
| virtualized | BodyBuilds | 194.00 | 194.00 | 0.00% | within budget |
| virtualized | Mounts | 98.00 | 98.00 | 0.00% | within budget |
| virtualized | Unmounts | 95.00 | 95.00 | 0.00% | within budget |
| themed-full-list | MountMilliseconds | 7,832.32 | 7,789.49 | -0.55% | within budget |
| themed-full-list | Milliseconds | 9,952.23 | 10,426.38 | 4.76% | within budget |
| themed-full-list | MountAllocatedBytes | 327,894,768.00 | 327,692,168.00 | -0.06% | within budget |
| themed-full-list | AllocatedBytes | 459,315,888.00 | 458,807,264.00 | -0.11% | within budget |
| themed-full-list | MountBodyBuilds | 1,022.00 | 1,022.00 | 0.00% | within budget |
| themed-full-list | BodyBuilds | 5,141.00 | 5,141.00 | 0.00% | within budget |
| themed-full-list | Mounts | 456.00 | 456.00 | 0.00% | within budget |
| themed-full-list | Unmounts | 658.00 | 658.00 | 0.00% | within budget |
| layout-editors | MountMilliseconds | 2,294.75 | 2,331.96 | 1.62% | within budget |
| layout-editors | Milliseconds | 20,254.51 | 19,250.80 | -4.96% | within budget |
| layout-editors | MountAllocatedBytes | 126,363,008.00 | 126,359,536.00 | -0.00% | within budget |
| layout-editors | AllocatedBytes | 1,113,058,160.00 | 1,113,021,344.00 | -0.00% | within budget |
| layout-editors | MountBodyBuilds | 1.00 | 1.00 | 0.00% | within budget |
| layout-editors | BodyBuilds | 50.00 | 50.00 | 0.00% | within budget |
| layout-editors | Mounts | 0.00 | 0.00 | 0.00% | within budget |
| layout-editors | Unmounts | 0.00 | 0.00 | 0.00% | within budget |
