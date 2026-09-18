# UI Framework documentation

The framework is a C# class library with a WPF renderer. This index describes the implementation as of September 17, 2026. It is a working prototype; APIs may change.

## Start here

| Guide | Covers |
| --- | --- |
| [Project quick start](../README.md) | Build, run, project structure, and first view. |
| [Release checklist](releasing.md) | Experimental release status, validation and owner decisions. |
| [Components](components.md) | Identity, props, local state, lifecycle, and scheduling. |
| [Bindings](bindings.md) | Two-way editing, immutable record projections, and custom adapters. |
| [Rendering performance](performance.md) | Derived observation, Memo inputs, and performance contracts. |
| [Measured results](performance-results.md) | Reference/optimized timings, allocations, checks, and measurement limitations. |
| [Stress lab](stress-lab.md) | Interactive controls, automated workloads, and comparison commands. |
| [Launchpad showcase](launchpad.md) | A release board with live editing, workflow actions, filters, and progress. |
| [Layout and styling](layout-styling.md) | Weighted rows, adaptive columns, alignment, and scoped control themes. |
| [Architecture graph](knowledge-graph.md) | Relationships between APIs, rendering, tests, and planned features. |
| [Decision record](decisions/0001-selective-rendering.md) | Why selective observation and explicit Memo inputs were added. |
| [Virtualization](virtualization.md) | Implemented behavior, state lifetime, test coverage and limitations. |
| [Virtualization roadmap](roadmap.md) | Remaining milestones and acceptance criteria. |

## Current behavior

1. A component's Body returns view descriptions. A body build is one invocation of that method, not a frame or complete control recreation.
2. A ViewSession records the observables read by configure/Body.
3. Changed state invalidates its readers. Bindings and derived selectors filter notifications when their result compares equal.
4. Each affected host queues a batched dispatcher update.
5. The renderer matches keys/types, updates retained controls, and creates/removes nodes as needed.
6. Memo inputs can skip unchanged parent-driven component builds. Local invalidations still run.
7. Removal detaches subscriptions before lifecycle cleanup hooks.

## Verification snapshot

The September 18, 2026 Release build and all 25 discoverable MSTest tests passed, including the new layout and styling checks. See [layout and styling](layout-styling.md) and [virtualization verification](virtualization.md). The following measurements are historical, from before virtualization and the test-project migration.

The latest implementation pass completed a clean Release build, 90 regression checks, and 21 full-workload stress assertions. The matched 50-operation comparison reduced body builds by 84.1%, UI-thread allocations by 30.6%, and elapsed time by 9.9%. These are recorded development-run measurements, not performance guarantees; the separate full-workload timing did not improve against its historical baseline. See the complete [measurement record](performance-results.md).

## Maintaining these docs

Update API contracts when behavior changes. Keep measured results dated and distinguish historical measurements from fresh test results. Edit knowledge-graph.json, then run tools/Update-KnowledgeGraph.ps1 to regenerate its Markdown map; use -Check to verify it is current. Planned items in the roadmap and graph are not implemented features.
