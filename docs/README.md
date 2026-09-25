# UI Framework documentation

The framework is a C# class library with a WPF renderer. This index describes the working source; dated verification records below identify the revisions actually tested. It is a working prototype; APIs may change.

## Start here

| Guide | Covers |
| --- | --- |
| [Project quick start](../README.md) | Build, run, project structure, and first view. |
| [Release checklist](releasing.md) | Experimental release status, validation and owner decisions. |
| [September 24 publication status](publication-status-2026-09-24.md) | Source commits, passing standard checks, and the editor timeout holding the next NuGet release. |
| [September 25 editor wait isolation](editor-wait-isolation-2026-09-25.md) | Captured text-services wait, native subtraction experiments, and setter-level timings. |
| [Editor COM cleanup diagnosis](editor-com-cleanup-2026-09-25.md) | Native UI/finalizer wait interaction and a controlled cleanup experiment. |
| [Application cleanup prototype](application-cleanup-policy-2026-09-25.md) | Application-owned idle scheduling, native COM lifetime checks, and paired diagnostic timings. |
| [September 23 review resolution](review-resolution-2026-09-23.md) | Hardening changes, verification evidence and remaining production gates. |
| [Validation and notification costs](validation-costs-2026-09-23.md) | Isolated timing, focused validation optimization, and limits of slowdown attribution. |
| [Failure and recovery contracts](error-recovery.md) | Render failures, explicit recovery, validation and notification behavior. |
| [Components](components.md) | Identity, props, local state, lifecycle, and scheduling. |
| [Bindings](bindings.md) | Two-way editing, immutable record projections, and custom adapters. |
| [Editors and selectors](editors.md) | Multiline/read-only text, bounded undo, masked passwords, and indexed selection. |
| [Rendering performance](performance.md) | Derived observation, Memo inputs, and performance contracts. |
| [Measured results](performance-results.md) | Reference/optimized timings, allocations, checks, and measurement limitations. |
| [Performance guardrails](performance-guardrails.md) | Paired baseline measurements, regression budgets, and CI/release gates. |
| [September 21 comparison](performance-2026-09-21.md) | Measured optimization results, 11-sample verification, and the approved themed budget. |
| [September 25 reconciliation investigation](themed-reconciliation-2026-09-25.md) | Rejected checkbox experiment, forward-row move optimization, and consumer cleanup opt-in evidence. |
| [September 20 performance exploration](performance-exploration-2026-09-20.md) | Virtualization measurements, dependency-tracking experiment, and tradeoffs of further techniques. |
| [September 18 comparison](performance-2026-09-18.md) | Detected regression, improvements, and the explicitly accepted temporary allocation budget. |
| [Accessibility labels](accessibility.md) | Contextual native automation names without changing visible control text. |
| [Native WPF interop](native-interop.md) | Retained native islands, ownership, cleanup, and migration gaps. |
| [Stress lab](stress-lab.md) | Interactive controls, automated workloads, and comparison commands. |
| [Launchpad showcase](launchpad.md) | A release board with live editing, workflow actions, filters, and progress. |
| [Layout and styling](layout-styling.md) | Weighted rows, adaptive columns, alignment, and scoped control themes. |
| [Navigation](navigation.md) | Typed history, retained screen state, lifetime, and entry transitions. |
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

The September 18, 2026 Release build and all 33 discoverable MSTest tests passed, including layout, styling, and navigation checks. The new visible navigation campaign passed 64 steps and 104 assertions; the full-list baseline passed 21 stress assertions. See [navigation](navigation.md) and [virtualization verification](virtualization.md). The following measurements are historical, from before virtualization and the test-project migration.

An earlier implementation pass completed a clean Release build, 90 regression checks, and 21 full-workload stress assertions. The matched 50-operation comparison reduced body builds by 84.1%, UI-thread allocations by 30.6%, and elapsed time by 9.9%. These are recorded development-run measurements, not performance guarantees; the separate full-workload timing did not improve against its historical baseline. See the complete [measurement record](performance-results.md).

## Maintaining these docs

Update API contracts when behavior changes. Keep measured results dated and distinguish historical measurements from fresh test results. Edit knowledge-graph.json, then run tools/Update-KnowledgeGraph.ps1 to regenerate its Markdown map; use -Check to verify it is current. Planned items in the roadmap and graph are not implemented features.
