"""Validate repeated native lifetime evidence; report trends, never promote a memory budget."""
import argparse
import json
import math
from pathlib import Path

METRICS = ('CollectedManagedBytes', 'NaturalManagedBytes', 'PrivateMemorySize64',
           'WorkingSet64', 'TotalCommittedBytes', 'HandleCount', 'GdiObjects', 'UserObjects')


def validate(report, campaign):
    if not report['Passed'] or not report['DispatcherShutdownFinished'] or not report['CleanupPolicyClosed'] or report['Mode'] != 'memory':
        raise ValueError('Incomplete memory process or shutdown')
    data = report['Result']
    if data['Kind'] != 'sustained-native-memory-observation' or data['ReleaseGate'] or data['NegativeControlDetected']:
        raise ValueError('Wrong evidence kind')
    for field, setting in [('Rows', 'rows'), ('CyclesPerBatch', 'cycles'), ('MeasuredBatches', 'batches'), ('WarmupBatches', 'warmups')]:
        if data[field] != campaign[setting]:
            raise ValueError('Workload mismatch')
    samples = data['Samples']
    if len(samples) != campaign['batches'] + campaign['warmups']:
        raise ValueError('Missing batches')
    previous = 0
    for index, sample in enumerate(samples):
        if sample['Batch'] != index or sample['Warmup'] != (index < campaign['warmups']):
            raise ValueError('Wrong batch order or warmup')
        if sample['Created'] <= previous or sample['Created'] != sample['Released'] or sample['BufferCreates'] != sample['BufferReleases']:
            raise ValueError('Missing work or unbalanced release')
        previous = sample['Created']
        if sample['NativeBytes'] or sample['WrongThreadReleases'] or any(sample['Alive'].values()):
            raise ValueError('Retained owned object/resource')
        if not all(k in sample['Alive'] for k in ('host', 'component', 'logicalPayload', 'nativeControl', 'nativePayload', 'popup')):
            raise ValueError('Missing ownership probes')
        for metric in METRICS:
            if not math.isfinite(sample[metric]) or sample[metric] < 0:
                raise ValueError('Invalid memory metric')
    return [s for s in samples if not s['Warmup']]


def summarize(directory):
    directory = Path(directory)
    read = lambda name: json.loads((directory / name).read_text(encoding='utf-8-sig'))
    campaign = read('campaign.json')
    if campaign['kind'] != 'native-lifetime-memory-observation-not-performance-gate':
        raise ValueError('Wrong campaign')
    window, negative = read('window.json'), read('negative.json')
    for report in (window, negative):
        if not report['Passed'] or not report['DispatcherShutdownFinished'] or not report['CleanupPolicyClosed']:
            raise ValueError('Window or negative-control process failed')
    if not negative['Result']['NegativeControlDetected'] or not any(s['Alive'].get('nativeControl', 0) > 0 for s in negative['Result']['Samples']):
        raise ValueError('Retention control did not detect deliberate roots')
    retention = None
    if campaign.get('includesRetention'):
        retention = read('retained.json')
        if not retention['Passed'] or not retention['DispatcherShutdownFinished'] or not retention['CleanupPolicyClosed']:
            raise ValueError('Retention checkpoint process failed')
        phases = retention['Result']['Samples']
        if [p['Phase'] for p in phases] != ['initial-viewport', 'all-rows-visited', 'half-keys-removed', 'all-keys-removed', 'host-disposed']:
            raise ValueError('Missing retention checkpoint')
        if phases[1]['Alive']['component'] != campaign['rows'] or phases[2]['Alive']['component'] != campaign['rows'] // 2:
            raise ValueError('Logical state workload changed')
        if any(any(p['Alive'].values()) or p['NativeBytes'] for p in phases[3:]):
            raise ValueError('Removed logical state retained')
    results, runtimes = [], set()
    for repetition in range(1, campaign['repetitions'] + 1):
        report = read(f'memory-{repetition}.json')
        runtimes.add(report['Runtime'])
        samples = validate(report, campaign)
        metrics = {}
        for metric in METRICS:
            values = [sample[metric] for sample in samples]
            n = len(values)
            center = (n - 1) / 2
            slope = sum((i - center) * value for i, value in enumerate(values)) / sum((i - center) ** 2 for i in range(n))
            metrics[metric] = dict(first=values[0], last=values[-1], minimum=min(values), maximum=max(values),
                                   change=values[-1]-values[0], slopePerBatch=slope)
        results.append(dict(repetition=repetition, measuredBatches=len(samples), metrics=metrics,
                            nativeMountsIncludingWarmup=report['Result']['Samples'][-1]['Created']))
    if len(runtimes) != 1:
        raise ValueError('Mismatched runtimes')
    return dict(kind=campaign['kind'], ownershipInvariantsPassed=True, negativeControlDetected=True,
                releaseGate=False, runtime=next(iter(runtimes)), runs=results,
                retentionCheckpoints=None if retention is None else retention['Result']['Samples'],
                interpretation='Forced-GC reachability and observed process trends; not a latency benchmark, leak-free proof or approved v1 memory budget.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory')
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    output = Path(args.output)
    if output.exists():
        raise SystemExit('Use a new output path to preserve evidence')
    output.write_text(json.dumps(summarize(args.directory), indent=2) + '\n', encoding='utf-8')
