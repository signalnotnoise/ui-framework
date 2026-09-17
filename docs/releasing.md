# Preparing an experimental release

The project name is ui-framework and the license is MIT, credited to UI Framework contributors. The candidate version is 0.1.0-alpha.1, defined in Directory.Build.props. The first release is intended as source for evaluation. Package generation is disabled until public package identity is chosen.

## Required owner decisions

- MIT licensing is selected and the approved license text is in LICENSE.
- Public repository: https://github.com/signalnotnoise/ui-framework. Verify the staged source files before uploading.
- For NuGet: choose unique package IDs and author metadata, set repository/license/readme metadata, enable packing only for the two libraries, and verify installation from a local package feed first. Do not publish packages with placeholder metadata.

## Validation

Local verification on September 17, 2026: Release build completed with zero warnings/errors; 19 MSTest tests, 15 visual-stress checks and 21 full-stress assertions passed. Full stress results are recorded in artifacts/release-validation/stress.json. Hosted CI has not run yet because no public repository has been created.

Run `./tools/Test-Release.ps1` from Windows PowerShell or PowerShell 7. The same checks are configured in .github/workflows/validate.yml. Local evidence is stored under artifacts/release-validation and is excluded from source control.

The workflow builds and validates only. It has read-only repository permissions and no publishing credentials. Its hosted run must still pass after the repository is created.

## Source release

Run `./tools/New-SourceArchive.ps1` to generate artifacts/ui-framework-0.1.0-alpha.1-source.zip. The script includes an explicit set of source/documentation directories and file types, validates required files, and excludes build outputs and user settings. It does not upload or publish the archive.

Exclude bin, obj, .vs, TestResults, artifacts, local environment files and user-specific IDE settings. Include source, project files, docs, tools, README, CHANGELOG, CONTRIBUTING and LICENSE. Review the release contents for accidental private data before upload. Git is initialized locally; the initial commit and push require a configured author and GitHub write authentication.

After validation and owner decisions are complete, create the public repository and an explicitly marked prerelease with a source archive. Describe it as experimental, Windows-only for rendering, requiring .NET 10. Do not present historical benchmarks as performance guarantees or claim focus/IME/accessibility verification.

No repository, tag, release or package is created by the validation script.
