"""Evidence validation rejects missing pairs, workload drift and wrong conditions."""
import copy
import importlib.util
import sys
from pathlib import Path

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("editor", Path(__file__).resolve().parents[1] / "tools/Summarize-EditorMount.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
data = {"kind": "editor-initialization-order-experiment-not-release-gate", "samples": 3, "tracePhases": False,
        "results": [{"sample": i, "InitializationOrder": mode, "warmup": i == 0, "PhaseTracing": False,
                     "Rows": 1000, "MixedOperations": 50, "MountBodyBuilds": 1, "BodyBuilds": 50}
                    for i in range(4) for mode in module.MODES]}
assert len(module.validate_records(data)) == 8
for label in ("missing", "duplicate", "warmup", "workload", "trace", "condition"):
    bad = copy.deepcopy(data)
    if label == "missing": bad["results"].pop()
    elif label == "duplicate": bad["results"].append(bad["results"][0])
    elif label == "warmup": bad["results"][0]["warmup"] = False
    elif label == "workload": bad["results"][0]["Rows"] = 500
    elif label == "trace": bad["results"][0]["PhaseTracing"] = True
    else: bad["results"][0]["InitializationOrder"] = "unknown"
    try:
        module.validate_records(bad)
    except ValueError:
        pass
    else:
        raise AssertionError(label + " must fail")
print("PASS: complete pairs accepted; missing/duplicate pairs, warmup, workload, tracing and condition drift rejected")
