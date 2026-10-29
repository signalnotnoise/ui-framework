"""Regression fixture: interleaved sampler/GC pauses and phase clipping."""
import importlib.util
import sys
from pathlib import Path

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("timing", Path(__file__).resolve().parents[1] / "tools/Summarize-TimingBottleneck.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
events = [
    {"EventName": "GC/SuspendEEStart", "ThreadID": 1, "Milliseconds": 1,
     "Payload": {"ClrInstanceID": "a", "Reason": "SuspendOther"}},
    {"EventName": "GC/SuspendEEStart", "ThreadID": 2, "Milliseconds": 2,
     "Payload": {"ClrInstanceID": "a", "Reason": "SuspendForGC"}},
    {"EventName": "GC/RestartEEStop", "ThreadID": 1, "Milliseconds": 3,
     "Payload": {"ClrInstanceID": "a"}},
    {"EventName": "GC/RestartEEStop", "ThreadID": 2, "Milliseconds": 10,
     "Payload": {"ClrInstanceID": "a"}},
]
intervals, unmatched = module.suspension_intervals(list(reversed(events)))
assert intervals == [(1, 3, "SuspendOther"), (2, 10, "SuspendForGC")]
assert unmatched == 0
assert module.overlap(2, 10, 5, 8) == 3
assert module.overlap(2, 10, 10, 15) == 0
assert module.overlap(2, 10, 0, 1) == 0
assert module.suspension_intervals(events[:1])[1] == 1
print("PASS: interleaved sampler/GC pauses stay separate, events sort, phase intervals clip and incomplete suspensions remain visible")

assert module.presentation_state({"PresentationPrimed": False}) is False
assert module.presentation_state({"Scenario": "layout-editors", "InitializationOrder": "source-first"}) is True
assert module.presentation_state({"Scenario": "layout-editors", "InitializationOrder": "host-first"}) is False
try:
    module.presentation_state({"Scenario": "layout-editors", "InitializationOrder": "unknown"})
except ValueError:
    pass
else:
    raise AssertionError("Unknown initialization condition must fail")
print("PASS: editor initialization conditions are explicit and unknown conditions fail")
