## Summary

Brief description of the change and why it was made.

## Checklist

- [ ] `dotnet build -c Release` passes with zero warnings
- [ ] `dotnet test` passes
- [ ] `dotnet csharpier check .` passes (formatting)
- [ ] `dotnet format style --verify-no-changes` passes (naming/style)
- [ ] `dotnet format analyzers --verify-no-changes` passes (Roslyn analyzers)
- [ ] Public API changes are reflected in README (run `dotnet test` to auto-update)
- [ ] Breaking changes are noted in the PR description
