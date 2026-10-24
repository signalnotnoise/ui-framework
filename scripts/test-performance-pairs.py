"""Check paired diagnostics reject incomplete data and exclude warmup processes."""
import importlib.util
import json
import tempfile
import sys

sys.dont_write_bytecode = True
from pathlib import Path

spec = importlib.util.spec_from_file_location("pairs", Path(__file__).resolve().parents[1] / "tools/Summarize-PerformancePairs.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
with tempfile.TemporaryDirectory() as location:
    root = Path(location)
    (root / "summary.json").write_text(json.dumps({"samples": 3, "rows": 1000, "operations": 50,
                                                 "comparisons": [{"scenario": "full-list"}]}))
    for sample in range(4):
        for side in ["baseline", "candidate"]:
            value = 100 if side == "baseline" else (100000 if sample == 0 else 110)
            record = {"Rows": 1000, "MixedOperations": 50, "Scenario": "full-list", "Runtime": "10.0.13"}
            record.update({name: value for name in ["MountMilliseconds", "Milliseconds", "MountAllocatedBytes", "AllocatedBytes"]})
            (root / f"full-list-{sample}-{side}.json").write_text(json.dumps(record))
    result = module.summarize(root)
    assert all(abs(r["medianPairedChangePercent"] - 10) < 0.001 and r["pairs"] == 3 for r in result["comparisons"])
    metadata = json.loads((root / "summary.json").read_text())
    metadata["scenarioRows"] = {"full-list": 32}
    (root / "summary.json").write_text(json.dumps(metadata))
    try:
        module.summarize(root)
        raise AssertionError("Accepted wrong per-scenario row count")
    except ValueError:
        pass
    metadata["scenarioRows"]["full-list"] = 1000
    (root / "summary.json").write_text(json.dumps(metadata))
    target = root / "full-list-2-candidate.json"
    data = json.loads(target.read_text()); data["Runtime"] = "different"; target.write_text(json.dumps(data))
    try:
        module.summarize(root)
        raise AssertionError("Accepted differing paired runtimes")
    except ValueError:
        pass
    target.unlink()
    try:
        module.summarize(root)
        raise AssertionError("Accepted a missing measured pair")
    except FileNotFoundError:
        pass
    metadata = {"samples": 3, "rows": 1000, "operations": 50, "supplementalNativeIslands": True,
                "comparisons": [{"scenario": "native-preserve"}]}
    (root / "summary.json").write_text(json.dumps(metadata))
    for sample in range(1, 4):
        for side in ["baseline", "candidate"]:
            record = {"Rows": 32, "MixedOperations": 50, "Scenario": "native-preserve", "Runtime": "10.0.13"}
            record.update({name: (100 if side == "baseline" else 110) for name in
                           ["MountMilliseconds", "Milliseconds", "MountAllocatedBytes", "AllocatedBytes"]})
            (root / f"native-preserve-{sample}-{side}.json").write_text(json.dumps(record))
    assert all(r["pairs"] == 3 for r in module.summarize(root)["comparisons"])
    metadata["scenarioRows"] = {"native-preserve": 32}
    (root / "summary.json").write_text(json.dumps(metadata))
    assert all(r["pairs"] == 3 for r in module.summarize(root)["comparisons"])
print("PASS: warmups excluded, paired change retained, mismatched runtime and missing pairs rejected")
