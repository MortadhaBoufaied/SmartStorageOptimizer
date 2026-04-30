import json
import os
import re
import sys
from typing import List, Optional


def classify(path: str, extension: Optional[str], snippet: Optional[str], candidate_labels: Optional[List[str]]):
    labels = candidate_labels or ["important", "archive", "temporary", "unknown"]
    lower_snippet = (snippet or "").lower()
    ext = (extension or os.path.splitext(path)[1]).lower()

    # Lightweight offline heuristic stub; replace later with ONNX/local transformer.
    if ext in {".log", ".tmp", ".cache"}:
        return "temporary", 0.88, ["ephemeral", "low-value"], "File extension strongly suggests temporary data."

    if ext in {".md", ".txt", ".docx", ".pdf"}:
        important_terms = ["invoice", "contract", "report", "design", "thesis", "meeting", "important", "cv", "resume"]
        if any(term in lower_snippet for term in important_terms):
            return "important", 0.79, ["document", "knowledge"], "Content indicates potentially important human-authored document."
        return "archive", 0.61, ["document"], "Document appears low-activity and suitable for archiving review."

    if ext in {".json", ".yaml", ".yml", ".toml"}:
        if re.search(r'package|dependencies|scripts|name', lower_snippet):
            return "important", 0.73, ["project-manifest"], "Manifest/config file should usually be preserved."

    return "unknown", 0.45, ["needs-review"], "No strong semantic signal."


def main():
    for raw in sys.stdin:
        raw = raw.strip()
        if not raw:
            continue
        if raw == "__shutdown__":
            break
        try:
            data = json.loads(raw)
            label, confidence, tags, explanation = classify(
                data.get("Path") or data.get("path", ""),
                data.get("Extension") or data.get("extension"),
                data.get("Snippet") or data.get("snippet"),
                data.get("CandidateLabels") or data.get("candidateLabels"),
            )
            payload = {
                "path": data.get("Path") or data.get("path", ""),
                "label": label,
                "confidence": confidence,
                "tags": tags,
                "explanation": explanation,
            }
        except Exception as exc:
            payload = {
                "path": "",
                "label": "unknown",
                "confidence": 0.0,
                "tags": ["error"],
                "explanation": f"agent error: {exc}",
            }
        sys.stdout.write(json.dumps(payload) + "
")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
