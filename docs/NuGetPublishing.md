# NuGet Publishing

ArcNET is a multi-package repository. Each publishable library now gets its version from the explicit manifest in `src/ArcNET.PackageVersions.props`.

The publishable library packages are expected to stay multiplatform across Windows, Linux, and macOS. Probe-style diagnostics and other local tooling are not part of that NuGet package portability contract.

## Tag Format

- `ArcNET.Core-v1.2.3`
- `ArcNET.Formats-v0.8.0`
- `ArcNET.Editor-v0.5.0`

When you create one of those tags on a commit and push it, the `nuget` GitHub Actions workflow packs and publishes only the matching package.

Before tagging a package release, update the corresponding entry in `src/ArcNET.PackageVersions.props` to the exact version you intend to publish.

## Local Commands

List the publishable packages:

```shell
dotnet Build.cs list-packages
```

Print the current manifest version for one package:

```shell
dotnet Build.cs package-version ArcNET.Core
```

Pack every publishable library into `artifacts/nuget`:

```shell
dotnet Build.cs pack
```

Pack one library by package id or project name:

```shell
dotnet Build.cs pack ArcNET.Core
dotnet Build.cs pack ArcNET.Editor
```

## GitHub Actions Publish Flow

1. Create and push a package tag such as `ArcNET.Core-v1.2.3`.
2. GitHub Actions runs `.github/workflows/nuget.yml`.
3. The workflow resolves the manifest version for the tagged package and fails fast if the tag version does not match `src/ArcNET.PackageVersions.props`.
4. The workflow restores tools, packs the matching project into `artifacts/nuget`, uploads the `.nupkg` and `.snupkg` artifacts, and pushes them to nuget.org using `NUGET_API_KEY`.

The main CI workflow also validates `dotnet Build.cs pack` on Ubuntu and macOS so the package layer stays portable even when the full repository test matrix remains Windows-first.

## Notes

- All package versions are explicit. Unrelated commits do not change a package version until its manifest entry changes.
- Packages can still diverge independently because each library has its own row in `src/ArcNET.PackageVersions.props`.
- The existing libraries already carry package metadata such as icon, README, license expression, symbols, and repository URLs in their project files.