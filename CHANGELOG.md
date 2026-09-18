# Changelog

## 0.1.0-alpha.1 — Unreleased

First experimental source and NuGet release candidate. Nothing has been published by this preparation step.

- Packable `SignalNotNoise.UI` and `SignalNotNoise.UI.Wpf` libraries, with README, MIT license, repository metadata, and portable symbol packages.
- Isolated local-feed WPF consumer validation and CI package artifacts.

- C# view descriptions and a Windows WPF renderer.
- Components with keyed identity, local state, lifecycle hooks and batched updates.
- Observable values/lists, projected bindings and derived values.
- Explicit component memoization.
- Virtualized lists with variable-height rows, saved component state and incremental collection updates.
- An interactive stress lab with full-list/virtualized modes and automated checks.
- MSTest regression coverage, architecture documentation and a maintained knowledge graph.

Known limitations include unverified visible-window focus/IME and accessibility behavior, quadratic work for some large shuffles, nontransactional rendering, and no navigation, animation or non-Windows backend. See docs/virtualization.md and docs/roadmap.md.
