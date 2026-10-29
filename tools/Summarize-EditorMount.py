"""Validate and summarize editor initialization-order experiments; never change gates."""
import argparse
import json
import statistics
from pathlib import Path

MODES = ("host-first", "source-first")
METRICS = ("MountMilliseconds", "Milliseconds", "MountAllocatedBytes", "AllocatedBytes")


def validate_records(data):
    if data.get("kind") != "editor-initialization-order-experiment-not-release-gate":
        raise ValueError("Wrong experiment kind")
    samples = data["samples"]
    rows = {}
    for row in data["results"]:
        key = (row["sample"], row["InitializationOrder"])
        if key in rows or key[1] not in MODES or not 0 <= key[0] <= samples:
            raise ValueError("Duplicate or invalid sample/condition")
        if row["warmup"] != (key[0] == 0) or row["PhaseTracing"] != data["tracePhases"]:
            raise ValueError("Wrong warmup or tracing condition")
        if (row["Rows"], row["MixedOperations"], row["MountBodyBuilds"], row["BodyBuilds"]) != (1000, 50, 1, 50):
            raise ValueError("Workload changed")
        rows[key] = row
    if set(rows) != {(i, mode) for i in range(samples + 1) for mode in MODES}:
        raise ValueError("Missing pair")
    return rows


def summarize(directory):
    directory = Path(directory)
    data = json.loads((directory / "comparison.json").read_text(encoding="utf-8-sig"))
    rows = validate_records(data)
    medians = {mode: {metric: statistics.median(rows[i, mode][metric] for i in range(1, data["samples"] + 1))
                      for metric in METRICS} for mode in MODES}
    result = {"kind": data["kind"], "tracePhases": data["tracePhases"], "samples": data["samples"], "medians": medians,
              "changePercent": {m: 100 * (medians[MODES[1]][m] / medians[MODES[0]][m] - 1) for m in METRICS},
              "pairedMountChangePercent": [100 * (rows[i, MODES[1]]["MountMilliseconds"] / rows[i, MODES[0]]["MountMilliseconds"] - 1)
                                           for i in range(1, data["samples"] + 1)]}
    if data["tracePhases"]:
        phases = {mode: [] for mode in MODES}
        geometry = None
        for (sample, mode), row in rows.items():
            trace = json.loads((directory / f"{sample}-{mode}.json.phases.json").read_text(encoding="utf-8-sig"))
            current = trace["Geometry"]["mount"]
            if current["Counts"].get("System.Windows.Controls.TextBox") != 1000:
                raise ValueError("Native editor count changed")
            if geometry is not None and current != geometry:
                raise ValueError("Mount geometry changed")
            geometry = current
            mount = [x for x in trace["Phases"] if x["Step"] == 0]
            names = [x["Phase"] for x in mount]
            if len(set(names)) != 7 or set(names) != {"cleanup-policy", "host-construction", "source-creation", "native-attachment", "theme-application", "dispatcher", "explicit-layout"}:
                raise ValueError("Missing or duplicate mount phase")
            if (names.index("source-creation") < names.index("host-construction")) != (mode == "source-first"):
                raise ValueError("Recorded initialization order is wrong")
            if sample:
                phases[mode].append({x["Phase"]: x for x in mount})
        result["geometry"] = geometry
        result["phases"] = {mode: {name: {metric: statistics.median(x[name][metric] for x in phases[mode])
                                                for metric in ("Milliseconds", "UserCpuMilliseconds", "KernelCpuMilliseconds", "AllocatedBytes", "GcPauseMilliseconds")}
                                   for name in phases[mode][0]} for mode in MODES}
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    path = Path(args.output)
    if path.exists():
        raise SystemExit("Use a new output path to retain evidence")
    path.write_text(json.dumps(summarize(args.directory), indent=2) + "\n", encoding="utf-8")
