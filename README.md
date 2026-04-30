# Smart Storage Optimizer

Smart Storage Optimizer is a .NET 8 desktop application that analyzes local storage, detects inactive files/folders/projects, suggests safe optimizations, and optionally uses a local Python AI agent for ambiguous content classification.

## Highlights
- .NET 8 WPF desktop app with MVVM + Generic Host
- Core rule engine for activity scoring and recommendations
- Plugin system for project/framework detection and project-specific cleanup
- Optional Python NLP/content classification agent via stdin/stdout JSON IPC
- SQLite-ready EF Core data model
- Safe action engine with dry-run, backup, recycle-bin/archive pattern, and audit logging
- Iteration log documenting 20 design refinement cycles

## Solution Layout
- `SmartStorage.UI` - WPF UI and view models
- `SmartStorage.Application` - orchestration services and DTOs
- `SmartStorage.Core` - scanners, analyzers, decisions, actions, safety
- `SmartStorage.Infrastructure` - local adapters / repositories / file system
- `SmartStorage.Abstractions` - interfaces and contracts
- `SmartStorage.Plugins` - detectors, cleaners, compressors
- `SmartStorage.AI` - Python agent bridge
- `SmartStorage.Data` - EF Core models and DbContext
- `SmartStorage.Tests` - example unit tests
- `python_ai` - local Python agent
- `docs` - architecture notes and 20-iteration refinement history

## Build
```bash
# Open in Visual Studio 2022+ or run from Windows SDK environment
# Restore packages
 dotnet restore
# Build
 dotnet build SmartStorageOptimizer.sln
```

## Run Python Agent
```bash
cd python_ai
python agent.py
```

## Notes
This starter is intentionally production-oriented and extensible. It provides a complete structure and many concrete implementations, but you should still adapt security, packaging, and OS-specific usage tracking hooks for your environment.
