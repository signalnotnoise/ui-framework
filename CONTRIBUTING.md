# Contributing

This is an experimental Windows UI framework. APIs and lifecycle contracts may change between prereleases.

## Development

Install the .NET 10 SDK on Windows. Clone or extract the repository, then run:

```powershell
dotnet restore "UI Framework.slnx"
dotnet test "tests/UI Framework.Checks"
dotnet run --project samples/Counter
```

Tests use MSTest and appear in Visual Studio Test Explorer. WPF test bodies must execute inside StaTestRunner.Run, including state/control construction and dispatcher work. Keep one declared type per file; group types by responsibility. See AGENTS.md for source organization.

## Changes

Keep the core independent of WPF. Include a regression test for behavior changes, update the relevant API contract, and update the knowledge graph when responsibilities change. Regenerate the graph with tools/Update-KnowledgeGraph.ps1.

Run tools/Test-Release.ps1 before proposing a release. It builds with warnings treated as errors, runs the regression tests, validates the graph, and runs both visual and full stress checks. Stop any running Release demo before rebuilding.

Report reproducible bugs with Windows/.NET versions, a minimal view or sequence of actions, expected behavior, and actual behavior. Do not include credentials, personal data, or private project files in reports.
