# CI/CD

This repo includes GitHub Actions workflows for continuous integration, package publishing, and documentation.

CI (build, pack, release)
- Workflow: .github/workflows/ci.yml
- Trigger: pushes and pull requests to main
- Steps:
  - Setup .NET (8.0.x, 9.0.x) and restore with lock file
  - Versioning via GitVersion (nuGetVersionV2 used for PackageVersion)
  - Build Release and pack all packable projects to ./artifacts
  - Upload .nupkg/.snupkg as an artifact
  - On main: create a git tag v{version} and GitHub Release with uploaded packages
  - Optional: Push to NuGet.org when NUGET_API_KEY is set
  - Optional: Push to GitHub Packages using GITHUB_TOKEN

Docs (DocFX → GitHub Pages)
- Workflow: .github/workflows/docs.yml
- Trigger: pushes to main
- Steps:
  - Setup .NET 9.x, install and run the docfx global tool
  - Build docs using docfx.json which generates _site/
  - Upload and deploy to GitHub Pages via actions/deploy-pages

Local docs preview
```powershell
# Install/Update DocFX globally (once)
dotnet tool update -g docfx

# From repo root: serve and auto-rebuild on changes
docfx --serve
```
- Open the printed localhost URL (default http://localhost:8080) and keep the process running; press Ctrl+C to stop.

Tips
- Keep docs in /docs; API XML is generated from src/*.csproj via docfx metadata.
- For custom navigation, edit docs/toc.yml and the root toc.yml.
- Ensure public API XML comments are complete for a rich API reference.
