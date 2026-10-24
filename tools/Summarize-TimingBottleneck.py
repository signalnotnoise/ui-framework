"""Correlate phase markers, UI sampled stacks and CLR pauses; never modify gates."""
import argparse
import collections
import json
from pathlib import Path


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def overlap(a, b, start, end):
    return max(0.0, min(b, end) - max(a, start))


def suspension_intervals(events):
    # The sampler can resume while a different thread is starting a GC pause.
    # A single global pending start would pair these unrelated requests wrongly.
    pending, intervals = {}, []
    for event in sorted(events, key=lambda item: item["Milliseconds"]):
        key = (event["ThreadID"], event["Payload"].get("ClrInstanceID"))
        if event["EventName"] == "GC/SuspendEEStart":
            if key in pending:
                raise ValueError("Nested suspension on the same runtime/thread")
            pending[key] = event
        elif event["EventName"] == "GC/RestartEEStop" and key in pending:
            start = pending.pop(key)
            intervals.append((start["Milliseconds"], event["Milliseconds"], start["Payload"]["Reason"]))
    return intervals, len(pending)


def profile(directory):
    directory = Path(directory)
    report = read(directory / "report.json")
    telemetry = read(directory / "report.json.phases.json")
    events = sorted(read(directory / "events.json"), key=lambda item: item["Milliseconds"])
    timeline = read(directory / "trace.speedscope.json")
    ui = telemetry["UiThreadId"]
    starts, phases = {}, []
    for event in events:
        if event["ProviderName"] != "SignalNotNoise-Comparison":
            continue
        payload = event["Payload"]
        key = (payload["phase"], int(payload["step"]))
        if event["ThreadID"] != ui:
            raise ValueError("Phase marker emitted off the UI thread")
        if payload["beginning"] == "True":
            if key in starts:
                raise ValueError("Overlapping phase markers")
            starts[key] = event["Milliseconds"]
        else:
            phases.append({"phase": key[0], "step": key[1], "start": starts.pop(key),
                           "end": event["Milliseconds"], "sampledFramesMs": collections.Counter()})
    if starts or len(phases) != len(telemetry["Phases"]):
        raise ValueError("Missing phase markers")
    sample = next(item for item in timeline["profiles"] if item["name"] == f"Thread ({ui})")
    stack, previous = [], sample["startValue"]
    targets = ["GetProcessDpiAwarenessFromWindow", "EnsureResourceChangeListener", "Monitor.Enter_Slowpath", "PollGC"]
    for event in sample["events"]:
        now = event["at"]
        if now > previous:
            names = [timeline["shared"]["frames"][index]["name"] for index in stack]
            present = [target for target in targets if any(target in name for name in names)]
            if present:
                for phase in phases:
                    duration = overlap(previous, now, phase["start"], phase["end"])
                    for target in present:
                        phase["sampledFramesMs"][target] += duration
        if event["type"] == "O":
            stack.append(event["frame"])
        else:
            stack.pop()
        previous = now
    suspensions, unmatched = suspension_intervals(events)
    contentions = [item for item in events if item["EventName"] == "Contention/Stop" and item["ThreadID"] == ui]
    grouped = {}
    for phase in phases:
        aggregate = grouped.setdefault(phase["phase"], {"wallMs": 0.0, "uiCpuMs": 0.0,
            "gcSuspensionMs": 0.0, "otherSuspensionMs": 0.0, "recordedContentionMs": 0.0,
            "sampledFramesMs": collections.Counter()})
        aggregate["wallMs"] += phase["end"] - phase["start"]
        data = next(item for item in telemetry["Phases"] if item["Phase"] == phase["phase"] and item["Step"] == phase["step"])
        aggregate["uiCpuMs"] += data["UserCpuMilliseconds"] + data["KernelCpuMilliseconds"]
        aggregate["sampledFramesMs"].update(phase["sampledFramesMs"])
        for a, b, reason in suspensions:
            key = "gcSuspensionMs" if reason in ("SuspendForGC", "SuspendForGCPrep") else "otherSuspensionMs"
            aggregate[key] += overlap(a, b, phase["start"], phase["end"])
        for event in contentions:
            duration = float(event["Payload"]["DurationNs"]) / 1e6
            aggregate["recordedContentionMs"] += overlap(event["Milliseconds"] - duration,
                event["Milliseconds"], phase["start"], phase["end"])
    return {"presentationInitialized": report["PresentationPrimed"], "uiThreadId": ui,
            "unmatchedSuspensionStarts": unmatched,
            "phaseCount": len(phases), "dpiCache": telemetry["DpiCache"], "geometry": telemetry["Geometry"],
            "phases": grouped}


def summarize(root):
    root = Path(root)
    cold, initialized = profile(root / "corrected-cold"), profile(root / "corrected-initialized")
    if cold["presentationInitialized"] or not initialized["presentationInitialized"]:
        raise ValueError("Wrong trace conditions")
    if cold["geometry"] != initialized["geometry"]:
        raise ValueError("Corrected-profile geometry differs")
    for name in ("Counter.dll", "UI Framework.dll", "UI Framework.Wpf.dll"):
        a = next(item["sha256"] for item in read(root / "corrected-cold/binary-manifest.json") if item["name"] == name)
        b = next(item["sha256"] for item in read(root / "corrected-initialized/binary-manifest.json") if item["name"] == name)
        if a != b:
            raise ValueError("Profile binaries differ")
    return {"interpretation": "Diagnostic profiles, not timing gates. UI sampled stack spans may include waits; inclusive frames overlap. CPU is quantized Windows thread accounting. GC/runtime suspensions and actual contention are separately reported.",
            "sameBinaries": True, "geometryMatches": True, "cold": cold, "initialized": initialized}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output)
    if output.exists():
        raise SystemExit("Use a new output path to preserve evidence")
    output.write_text(json.dumps(summarize(args.directory), indent=2) + "\n", encoding="utf-8")
