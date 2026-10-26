from __future__ import annotations

from typing import Any


def workflow_summary(report: dict[str, Any]) -> dict[str, Any]:
    """Summarize the workflow verdict and canonical leaves without counting raw echoes."""
    summary = {"ok": report.get("ok")}
    for key in ("action", "route", "mode", "project", "compiled", "noCompilationReason",
                "reportPath", "reportSha256", "summary", "duration", "wallSeconds"):
        if key in report:
            summary[key] = report[key]
    error = report.get("error")
    results = report.get("results")
    if isinstance(error, dict):
        concise = {key: error[key] for key in ("code", "message") if key in error}
        details = error.get("details")
        if isinstance(details, dict):
            if error.get("code") == "OFFLINE_TEST_FAILED":
                if "summary" not in summary and isinstance(details.get("summary"), dict):
                    summary["summary"] = details["summary"]
                if results is None:
                    results = details.get("results")
            concise["details"] = {key: value for key, value in details.items() if value is None or type(value) in {str, int, float, bool}}
            original = details.get("originalWorkflowError")
            if isinstance(original, dict):
                concise["details"]["originalWorkflowError"] = {key: original[key] for key in ("code", "message") if key in original}
        summary["error"] = concise
    if isinstance(results, list):
        summary["nonpassingTests"] = [
            item for item in results
            if isinstance(item, dict) and item.get("Status") != "Passed"
        ]
    return summary
