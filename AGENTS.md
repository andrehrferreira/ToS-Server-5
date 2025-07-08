# AGENTS Instructions

These guidelines apply to the entire repository.

## Coding Style
- Indent C# code using four spaces and follow the naming rules in `.editorconfig`.
- Ensure every file ends with a newline.

## Programmatic Checks
- Build the project to verify compilation:
  ```bash
  pnpm build
  ```
- Run the embedded tests:
  ```bash
  dotnet run --project GameServer.csproj
  ```
  Tests execute automatically at startup when running in Debug mode.

## Pull Request Notes
- Summaries should outline the main changes.
- Include test output in the PR description.
