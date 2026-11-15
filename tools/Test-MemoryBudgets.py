"""Evaluate frozen, workload-specific memory limits without changing raw evidence."""
import argparse
import hashlib
import importlib.util
import json
import math
import statistics
import sys
from pathlib import Path

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('native_memory', Path(__file__).with_name('Summarize-NativeMemory.py'))
native = importlib.util.module_from_spec(spec)
spec.loader.exec_module(native)


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def trends(samples, limits, width):
    require(len(samples) >= width * 4, 'Insufficient nonoverlapping resource windows')
    middle = len(samples) // 2
    results = {}
    for metric, budget in limits.items():
        values = [s[metric] for s in samples]
        require(all(type(v) in (int, float) and math.isfinite(v) and v >= 0 for v in values), 'Invalid metric: ' + metric)
        require(set(budget) == {'peak', 'growth', 'lateGrowth'} and all(type(v) in (int, float) and v >= 0 and math.isfinite(v) for v in budget.values()), 'Invalid limit: ' + metric)
        first = statistics.median(values[:width])
        mid = statistics.median(values[middle:middle+width])
        last = statistics.median(values[-width:])
        observed = dict(peak=max(values), growth=last-first, lateGrowth=last-mid)
        results[metric] = dict(observed=observed, limits=budget, firstMedian=first,
                               middleMedian=mid, lastMedian=last,
                               exceeded=[k for k, value in observed.items() if value > budget[k]])
    return results


def raw_matches(folder, samples):
    raw = [read(p) for p in sorted(Path(folder).glob('*.json'))]
    require(raw == samples, 'Raw samples differ from reconstructed report')


def browser_samples(report, folder, mode, cycles, policy):
    require(report['Passed'] and report['DispatcherShutdownFinished'] and report['CleanupPolicyClosed'], 'Browser shutdown failed')
    require(report['Mode'] == mode and not report['ReleaseGate'], 'Wrong browser report')
    d = report['Result']; samples = d['Samples']
    require(d['Passed'] and d['Mode'] == mode and d['Cycles'] == cycles and d['Warmups'] == policy['warmups'] and not d['ReleaseGate'], 'Browser workload mismatch')
    require(d['NegativeControlDetectedAndReleased'] and 'parent-owner' in d['NegativeControl']['RetainedBeforeRelease'] and not d['NegativeControl']['SurvivorsAfterRelease'], 'Missing deliberate-retention sensitivity')
    require(len(samples) == cycles, 'Missing browser cycles')
    reporting = d['ReportingRetention']
    require(reporting['Protocol'] == 'bounded-sample-files-v1' and reporting['Samples'] == cycles
            and reporting['VerifiedCollectedSamples'] == cycles and reporting['MaximumPendingWeakProbes'] == 1
            and reporting['SampleSurvivors'] == 0 and reporting['MeasurementBeforeReportReload'], 'Unbounded browser reporting')
    raw_matches(Path(folder)/'bounded-samples', samples)
    versions = set()
    live_private = []; live_counts = []
    for i, s in enumerate(samples):
        require(s['Cycle'] == i and s['Warmup'] == (i < policy['warmups']) and not s['Survivors'], 'Browser cycle or ownership failure')
        e = s['Evidence']; versions.add(e['Runtime'])
        require(e['AllCapturedProcessesExited'] and e['RemainingProcesses'] == 0
                and e['ParentReleases'] == 1 and e['ChildReleases'] == 1 and e['DeferralsCompleted'] == 1, 'Browser resource release failure')
        processes = e['Processes']
        require(len(processes) >= 2 and len({p['Id'] for p in processes}) == len(processes)
                and any(p['Id'] == e['BrowserPid'] for p in processes), 'Missing isolated process identity')
        require(all(type(p['PrivateMemorySize64']) is int and p['PrivateMemorySize64'] >= 0 for p in processes), 'Invalid browser private bytes')
        live_counts.append(len(processes)); live_private.append(sum(p['PrivateMemorySize64'] for p in processes))
    require(len(versions) == 1, 'Browser runtime changed during run')
    return [s['Collected'] for s in samples if not s['Warmup']], dict(
        runtime=next(iter(versions)), peakLiveProcesses=max(live_counts),
        peakLivePrivateBytes=max(live_private),
        exceeded=[name for name, value, limit in (
            ('liveProcesses', max(live_counts), policy['maximumLiveProcesses']),
            ('livePrivateBytes', max(live_private), policy['maximumLivePrivateBytes'])) if value > limit])


def evaluate(root, policy):
    root = Path(root); width = policy['medianWindow']; runs = []; evidence_errors = []
    require(policy['schemaVersion'] == 1 and type(width) is int and width >= 1, 'Unsupported policy')
    sources = []; runtimes = set(); browser_versions = set(); sdks = set()
    for rows in policy['native']['rows']:
        folder = root/f'native-{rows}'
        try:
            c = read(folder/'campaign.json')
            sdks.add(c['sdk'])
            require(c['rows'] == rows and c['batches'] >= policy['native']['minimumMeasuredBatches']
                    and c['cycles'] == policy['native']['cyclesPerBatch'] and c['warmups'] == policy['native']['warmups']
                    and c['repetitions'] >= policy['minimumRepetitions'] and c['includesRetention']
                    and c['inputObservation'] == 'window-routed-input-v1'
                    and c['reportingProtocol'] == 'bounded-sample-files-v1', 'Native campaign not eligible')
            native.summarize(folder)  # Includes window, deliberate-root and visited-state/removal controls.
            sources.append(read(folder/'source-manifest.json'))
            for rep in range(1, c['repetitions']+1):
                r = read(folder/f'memory-{rep}.json'); runtimes.add(r['Runtime'])
                samples = native.validate(r, c); raw_matches(folder/f'memory-{rep}.json.samples', r['Result']['Samples'])
                interaction = native.input_summary(r['Result']['Samples'])
                require(interaction['activationEvidenceAvailable'] and not interaction['observed'], f'native-{rows}/{rep}: interaction or missing activation evidence')
                metrics = trends(samples, policy['native']['metrics'], width)
                runs.append(dict(case=f'native-{rows}/{rep}', metrics=metrics, passed=not any(m['exceeded'] for m in metrics.values())))
        except (ValueError, KeyError, TypeError, OSError) as error:
            evidence_errors.append(f'native-{rows}: {error}')
    folder = root/'browser'
    try:
        c = read(folder/'campaign.json')
        sdks.add(c['sdk'])
        require(c['kind'] == 'real-webview-local-page-acceptance' and not c['releaseGate']
                and set(c['modes']) == set(policy['browser']['modes']) and len(c['modes']) == len(policy['browser']['modes'])
                and c['repetitions'] >= policy['minimumRepetitions']
                and c['retentionCycles'] >= policy['browser']['minimumMeasuredCycles'] + policy['browser']['warmups'], 'Browser campaign not eligible')
        sources.append(read(folder/'source-manifest.json'))
        for rep in range(1, c['repetitions']+1):
            for mode in policy['browser']['modes']:
                case = folder/f'{rep}-{mode}'; r = read(case/'report.json'); runtimes.add(r['DotNet'])
                samples, live = browser_samples(r, case, mode, c['retentionCycles'], policy['browser'])
                browser_versions.add(live['runtime'])
                metrics = trends(samples, policy['browser']['metrics'], width)
                runs.append(dict(case=f'browser/{rep}-{mode}', metrics=metrics, live=live,
                                 passed=not live['exceeded'] and not any(m['exceeded'] for m in metrics.values())))
    except (ValueError, KeyError, TypeError, OSError) as error:
        evidence_errors.append(f'browser: {error}')
    if len(runtimes) != 1 or len(browser_versions) != 1 or len(sdks) != 1:
        evidence_errors.append('Missing or mismatched SDK/.NET/browser runtime versions')
    shared = []
    for manifest in sources:
        shared.append({e['path'].replace('\\', '/'): e['sha256'].lower() for e in manifest
                       if e['path'].replace('\\', '/').startswith(('UI Framework/', 'UI Framework.Wpf/'))})
    if len(shared) != 3 or not shared[0] or any(s != shared[0] for s in shared):
        evidence_errors.append('Missing or mismatched framework sources across campaigns')
    return dict(policy=policy['policy'], policyStatus=policy['status'], releaseGate=False,
                passed=not evidence_errors and bool(runs) and all(r['passed'] for r in runs),
                evidenceErrors=evidence_errors, sdk=sorted(sdks), dotNet=sorted(runtimes), webView=sorted(browser_versions), runs=runs,
                interpretation='Scoped local acceptance; no universal leak-freedom, arbitrary-site browser budget, or hosted release approval.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory'); parser.add_argument('--policy', default='tools/memory-budgets.json'); parser.add_argument('--output', required=True)
    args = parser.parse_args(); output = Path(args.output)
    if output.exists(): raise SystemExit('Use a new output file to preserve evidence')
    result = evaluate(args.directory, read(args.policy))
    result['policySha256'] = hashlib.sha256(Path(args.policy).read_bytes()).hexdigest()
    output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(passed=result['passed'], cases=len(result['runs']), evidenceErrors=result['evidenceErrors'])))
    raise SystemExit(0 if result['passed'] else 1)
