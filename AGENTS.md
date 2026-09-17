# Source organization

- Use one declared type per C# file, including records, enums, interfaces and test helpers. Name the file after the type.
- Group files by responsibility (for example Components, Models, Rendering and Virtualization). Keep the core independent of WPF.
- Keep platform lifecycle, control creation, virtualization and application behavior in their respective layers.
- Add focused regression checks for behavior changes; retain a full-list baseline when measuring virtualization.
- Update the relevant documentation and knowledge graph when architectural responsibilities move.
