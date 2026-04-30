# Final Architecture

Smart Storage Optimizer is a hybrid intelligent local desktop agent built with .NET 8 (WPF UI + hosted services) and an optional Python content analysis process. Its primary goals are:

1. Detect unused files, folders, and entire projects.
2. Recognize common frameworks and project types safely.
3. Recommend the right action (`keep`, `compress`, `clean`, `delete`, `review`) with explainability.
4. Keep destructive operations reversible via backup and audit logs.
5. Run fully offline and locally for privacy.

## Core Principles
- **Safety first**: actions default to review/preview and require confirmation.
- **Rules before AI**: heuristics cover the majority of cases, AI only handles ambiguity.
- **Plugin-driven project understanding**: detectors identify framework/project layouts; cleaners target generated artifacts only.
- **Local-only**: no network required for scanning or AI.

## Framework Detection Strategy
The current solution includes detectors for:
- .NET (`*.sln`, `*.csproj`)
- Node.js (`package.json`)
- Python (`pyproject.toml`, `requirements.txt`, top-level `*.py`)
- Flutter (`pubspec.yaml`)

It is designed so you can add more detectors easily for Java, Maven, Gradle, Rust, Go, Unity, Unreal, Android, iOS, React Native, Angular, Vue, Laravel, Django, etc.

## Unused Detection Strategy
The agent combines:
- Last modification time
- Last tracked access time (custom tracker)
- Average access interval
- Frequency score
- Generated/cache/build markers
- File size and file type
- Optional AI content label for ambiguous documents

A high final score means an item is more likely to be inactive / cold / reclaimable.
