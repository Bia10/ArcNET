# Contributing to ArcNET

Thank you for considering contributing!

## How to Contribute

1. Fork the repository and create a feature branch from `main`.
2. Run `dotnet tool restore` to install local tools (CSharpier and dotnet-coverage).
3. Ensure your code passes all checks:
   ```shell
   dotnet Build.cs build
   dotnet Build.cs test
   dotnet Build.cs pack
   dotnet Build.cs format-check
   ```
4. Open a Pull Request against `main` with a clear description of the change.

Packable NuGet libraries are expected to stay portable across Windows, Linux, and macOS. Probe-style diagnostics and other local tooling are allowed to stay platform-specific, but package-facing code should not drift into a Windows-only contract.

## Code Style

This project enforces formatting via **CSharpier** (opinionated C# formatter) and **`dotnet format`** (code style analyzers). CI will reject PRs with formatting violations.

- **Auto-format before committing**: `dotnet csharpier format .` first (fixes whitespace/brace style), then `dotnet format style && dotnet format analyzers` (fixes naming/usings). **Order matters**: always run CSharpier first. Never run bare `dotnet format` — the `whitespace` sub-check overwrites CSharpier's Allman-style brace formatting.
- IDE integration: Install the CSharpier extension for [VS Code](https://marketplace.visualstudio.com/items?itemName=csharpier.csharpier-vscode), [Visual Studio](https://marketplace.visualstudio.com/items?itemName=csharpier.CSharpier), or [Rider](https://plugins.jetbrains.com/plugin/18243-csharpier) for format-on-save.

## Reporting Issues

Use [GitHub Issues](https://github.com/Bia10/ArcNET/issues) for bugs and feature requests.
