"""Describe paired benchmark variation without changing any performance gate."""
import argparse
import json
import math
import statistics
from pathlib import Path


def summarize(directory):
    directory = Path(directory)
    summary = json.loads((directory / "summary.json").read_text(encoding="utf-8-sig"))
    records = []
    for scenario in sorted({c["scenario"] for c in summary["comparisons"]}):
        pairs = []
        for sample in range(1, summary["samples"] + 1):
            sides = {}
            for side in ["baseline", "candidate"]:
                r = json.loads((directory / f"{scenario}-{sample}-{side}.json").read_text(encoding="utf-8-sig"))
                # Initial supplemental archives predate explicit scenarioRows.
                default_rows = 32 if summary.get("supplementalNativeIslands") and scenario in ("native-inherit", "native-preserve") else summary["rows"]
                expected_rows = summary.get("scenarioRows", {}).get(scenario, default_rows)
                if r["Rows"] != expected_rows or r["MixedOperations"] != summary["operations"] or r["Scenario"] != scenario:
                    raise ValueError("Mismatched benchmark workload")
                sides[side] = r
            if sides["baseline"]["Runtime"] != sides["candidate"]["Runtime"]:
                raise ValueError("Mismatched paired runtimes")
            pairs.append(sides)
        for metric in ["MountMilliseconds", "Milliseconds", "MountAllocatedBytes", "AllocatedBytes"]:
            changes = []
            for pair in pairs:
                left, right = pair["baseline"][metric], pair["candidate"][metric]
                if not math.isfinite(left) or not math.isfinite(right) or left <= 0 or right < 0:
                    raise ValueError("Invalid paired metric")
                changes.append(100 * (right / left - 1))
            records.append({"scenario": scenario, "metric": metric,
                            "pairedChangesPercent": changes,
                            "medianPairedChangePercent": statistics.median(changes),
                            "minimumPairedChangePercent": min(changes),
                            "maximumPairedChangePercent": max(changes),
                            "positivePairs": sum(v > 0 for v in changes),
                            "pairs": len(changes)})
    return {"source": str(directory), "identicalBinaryControl": summary.get("identicalBinaryControl", False),
            "interpretation": "Descriptive paired variation; not a replacement gate, confidence interval or causal attribution.",
            "comparisons": records}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output)
    if output.exists():
        raise SystemExit("Use a new output path to preserve prior evidence.")
    output.write_text(json.dumps(summarize(args.directory), indent=2) + "\n", encoding="utf-8")
