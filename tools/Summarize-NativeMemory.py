"""Validate repeated native lifetime evidence; report trends, never promote a memory budget."""
import argparse
import json
import math
from pathlib import Path

METRICS = ('CollectedManagedBytes', 'NaturalManagedBytes', 'PrivateMemorySize64',
           'WorkingSet64', 'TotalCommittedBytes', 'HandleCount', 'GdiObjects', 'UserObjects')


def input_summary(samples):
    return dict(observed=any(s['Input']['PointerEverInside'] or s['Input']['MouseMoves']
                            or s['Input']['MouseButtons'] or s['Input']['KeyDowns']
                            or s['Input'].get('Activations', 0) or s['Input'].get('IsActive', False)
                            or s['Input'].get('HasKeyboardFocus', False) for s in samples),
                activationEvidenceAvailable=all('Activations' in s['Input'] for s in samples),
                last=samples[-1]['Input'])


def validate_retention(report, rows):
    """Check owned payload accounting separately from noisy process memory."""
    if (not report['Passed'] or not report['DispatcherShutdownFinished']
            or not report['CleanupPolicyClosed'] or report['Mode'] != 'retained'):
        raise ValueError('Retention checkpoint process failed')
    data = report['Result']
    if (data['Kind'] != 'logical-row-retention-checkpoints' or data['Rows'] != rows
            or data['ReleaseGate'] or data['WholeApplicationMemoryBudget']):
        raise ValueError('Wrong retention workload or budget claim')
    phases = data['Samples']
    if [p['Phase'] for p in phases] != ['initial-viewport', 'all-rows-visited',
            'half-keys-removed', 'all-keys-removed', 'host-disposed']:
        raise ValueError('Missing retention checkpoint')
    for index, phase in enumerate(phases):
        context = phase['Phase']
        alive = phase['Alive']
        required = ('component', 'logicalPayload', 'nativeControl', 'nativePayload', 'popup')
        if not all(key in alive for key in required):
            raise ValueError(f'{context}: missing ownership probes')
        if any(type(value) is not int or value < 0 for value in alive.values()):
            raise ValueError(f'{context}: invalid live-object count')
        descriptions = (rows, rows, rows // 2, 0, 0)[index]
        if phase['Descriptions'] != descriptions:
            raise ValueError(f'{context}: description workload changed')
        logical, native = alive['component'], alive['nativeControl']
        if index == 0:
            if not 0 < logical < rows // 2:
                raise ValueError(f'{context}: initial logical realization is not bounded')
        elif logical != descriptions:
            raise ValueError(f'{context}: visited or removed logical state mismatch')
        if alive['logicalPayload'] != logical or phase['LogicalPayloadBytes'] != logical * 8192:
            raise ValueError(f'{context}: logical payload accounting mismatch')
        if index < 3 and not 0 < native < rows // 2:
            raise ValueError(f'{context}: native realization is not bounded')
        if native > logical or alive['nativePayload'] != native or alive['popup'] != native:
            raise ValueError(f'{context}: native ownership accounting mismatch')
        if phase['NativeBytes'] != native * 32768:
            raise ValueError(f'{context}: owned native byte accounting mismatch')
        if index >= 3 and any(alive.values()):
            raise ValueError(f'{context}: removed logical state retained')
    return phases


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
    if campaign.get('inputObservation'):
        previous = [0, 0, 0]
        previous_activations = 0
        activation_fields = ('Activations', 'IsActive', 'HasKeyboardFocus')
        has_activation = any(key in samples[0].get('Input', {}) for key in activation_fields) if samples else False
        for sample in samples:
            observed = sample.get('Input', {})
            if observed.get('Protocol') != campaign['inputObservation'] or type(observed.get('PointerEverInside')) is not bool:
                raise ValueError('Missing window input observation')
            counts = [observed.get(key) for key in ('MouseMoves', 'MouseButtons', 'KeyDowns')]
            if any(type(value) is not int or value < old for value, old in zip(counts, previous)):
                raise ValueError('Invalid cumulative input counts')
            previous = counts
            if has_activation or any(key in observed for key in activation_fields):
                if (not has_activation or type(observed.get('Activations')) is not int
                        or observed['Activations'] < previous_activations
                        or any(type(observed.get(key)) is not bool for key in activation_fields[1:])):
                    raise ValueError('Invalid or incomplete activation observation')
                previous_activations = observed['Activations']
    reporting = data.get('ReportingRetention')
    if campaign.get('reportingProtocol') and (reporting is None or reporting['Protocol'] != campaign['reportingProtocol']):
        raise ValueError('Missing or mismatched reporting protocol')
    if reporting is not None:
        if (reporting['Protocol'] not in ('release-reporting-roots-v1', 'bounded-sample-files-v1')
                or reporting['Samples'] != len(samples) or reporting['SampleSurvivors'] != 0
                or not reporting['MeasurementBeforeReportReload']):
            raise ValueError('Incomplete reporting-root isolation')
        if reporting['Protocol'] == 'bounded-sample-files-v1' and (
                reporting['VerifiedCollectedSamples'] != len(samples) or reporting['MaximumPendingWeakProbes'] != 1):
            raise ValueError('Incomplete bounded reporting verification')
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
        validate_retention(retention, campaign['rows'])
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
                            reportingRetention=report['Result'].get('ReportingRetention'),
                            inputObservation=None if not campaign.get('inputObservation') else input_summary(samples),
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
