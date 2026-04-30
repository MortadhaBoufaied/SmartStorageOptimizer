# python_ai

This folder contains the optional local Python content analysis agent. It communicates with the .NET app through line-delimited JSON over stdin/stdout.

## Usage
```bash
python agent.py
```

Each input line is either:
- a JSON request with fields like `path`, `extension`, `snippet`, `candidateLabels`
- `__shutdown__` to exit gracefully
