"""Reject incomplete or misleading sustained native lifetime evidence."""
import copy
import importlib.util
import sys
from pathlib import Path

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('memory', Path(__file__).resolve().parents[1] / 'tools/Summarize-NativeMemory.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
campaign = dict(rows=500, cycles=4, warmups=1, batches=5)
samples = []
for i in range(6):
    sample = dict(Batch=i, Warmup=i == 0, Created=(i+1)*76, Released=(i+1)*76,
                  BufferCreates=(i+1)*76, BufferReleases=(i+1)*76, NativeBytes=0, WrongThreadReleases=0,
                  Alive={k: 0 for k in ('host', 'component', 'logicalPayload', 'nativeControl', 'nativePayload', 'popup')})
    sample.update({k: 1000 for k in module.METRICS})
    samples.append(sample)
report = dict(Passed=True, DispatcherShutdownFinished=True, CleanupPolicyClosed=True, Mode='memory',
              Result=dict(Kind='sustained-native-memory-observation', ReleaseGate=False,
                          NegativeControlDetected=False, Rows=500, CyclesPerBatch=4,
                          MeasuredBatches=5, WarmupBatches=1, Samples=samples))
assert len(module.validate(report, campaign)) == 5
input_campaign = {**campaign, 'inputObservation': 'window-routed-input-v1'}
observed_report = copy.deepcopy(report)
for sample in observed_report['Result']['Samples']:
    sample['Input'] = dict(Protocol='window-routed-input-v1', PointerEverInside=False,
                          MouseMoves=0, MouseButtons=0, KeyDowns=0)
observed_samples = module.validate(observed_report, input_campaign)
assert not module.input_summary(observed_samples)['observed']
observed_report['Result']['Samples'][-1]['Input']['MouseMoves'] = 1
# Input does not invalidate ownership; it must invalidate a claim of no observed input.
assert module.input_summary(module.validate(observed_report, input_campaign))['observed']
try: module.validate(report, input_campaign)
except ValueError: pass
else: raise AssertionError('Missing input evidence must not be reported as untouched')
activation_report = copy.deepcopy(observed_report)
for sample in activation_report['Result']['Samples']:
    sample['Input'].update(MouseMoves=0, Activations=0, IsActive=False, HasKeyboardFocus=False)
activation_report['Result']['Samples'][-1]['Input'].update(Activations=1, IsActive=True, HasKeyboardFocus=True)
activation_summary = module.input_summary(module.validate(activation_report, input_campaign))
assert activation_summary['observed'] and activation_summary['activationEvidenceAvailable']
assert not module.input_summary(observed_samples)['activationEvidenceAvailable']
for mutation in ('missing', 'decreasing', 'wrong_type'):
    invalid = copy.deepcopy(activation_report)
    if mutation == 'missing': del invalid['Result']['Samples'][-1]['Input']['HasKeyboardFocus']
    elif mutation == 'decreasing': invalid['Result']['Samples'][-2]['Input']['Activations'] = 2
    else: invalid['Result']['Samples'][-1]['Input']['IsActive'] = 'false'
    try: module.validate(invalid, input_campaign)
    except ValueError: pass
    else: raise AssertionError('Invalid activation evidence was accepted: ' + mutation)
with_reporting = copy.deepcopy(report)
with_reporting['Result']['ReportingRetention'] = dict(Protocol='release-reporting-roots-v1',
    Samples=6, SampleSurvivors=0, MeasurementBeforeReportReload=True)
assert len(module.validate(with_reporting, campaign)) == 5
with_reporting['Result']['ReportingRetention']['SampleSurvivors'] = 1
try: module.validate(with_reporting, campaign)
except ValueError: pass
else: raise AssertionError('retained reporting samples must fail isolation')
bounded_report = copy.deepcopy(report)
bounded_campaign = {**campaign, 'reportingProtocol': 'bounded-sample-files-v1'}
bounded_report['Result']['ReportingRetention'] = dict(Protocol='bounded-sample-files-v1',
    Samples=6, SampleSurvivors=0, MeasurementBeforeReportReload=True,
    VerifiedCollectedSamples=6, MaximumPendingWeakProbes=1)
assert len(module.validate(bounded_report, bounded_campaign)) == 5
for case in ('missing', 'unverified', 'protocol'):
    bad = copy.deepcopy(bounded_report)
    if case == 'missing': del bad['Result']['ReportingRetention']
    elif case == 'unverified': bad['Result']['ReportingRetention']['VerifiedCollectedSamples'] = 5
    else: bad['Result']['ReportingRetention']['Protocol'] = 'release-reporting-roots-v1'
    try: module.validate(bad, bounded_campaign)
    except ValueError: pass
    else: raise AssertionError(case + ' bounded reporting evidence must fail')
for case in ('shutdown', 'missing', 'warmup', 'workload', 'release', 'native', 'managed', 'probe', 'metric', 'gate'):
    bad = copy.deepcopy(report)
    row = bad['Result']['Samples'][1]
    if case == 'shutdown': bad['DispatcherShutdownFinished'] = False
    elif case == 'missing': bad['Result']['Samples'].pop()
    elif case == 'warmup': row['Warmup'] = True
    elif case == 'workload': bad['Result']['Rows'] = 100
    elif case == 'release': row['Released'] -= 1
    elif case == 'native': row['NativeBytes'] = 32768
    elif case == 'managed': row['Alive']['nativeControl'] = 1
    elif case == 'probe': del row['Alive']['component']
    elif case == 'metric': row['PrivateMemorySize64'] = float('nan')
    else: bad['Result']['ReleaseGate'] = True
    try: module.validate(bad, campaign)
    except ValueError: pass
    else: raise AssertionError(case + ' must fail')
print('PASS: native memory validation rejects missing work, leaks, incomplete shutdown and false gate claims')

# Model real checkpoint accounting, then corrupt one independent guarantee at a time.
phases = []
for name, descriptions, logical, native in (
        ('initial-viewport', 500, 10, 10), ('all-rows-visited', 500, 500, 10),
        ('half-keys-removed', 250, 250, 10), ('all-keys-removed', 0, 0, 0),
        ('host-disposed', 0, 0, 0)):
    phases.append(dict(Phase=name, Descriptions=descriptions,
        Alive=dict(component=logical, logicalPayload=logical, nativeControl=native,
                   nativePayload=native, popup=native),
        LogicalPayloadBytes=logical * 8192, NativeBytes=native * 32768))
retention = dict(Passed=True, DispatcherShutdownFinished=True, CleanupPolicyClosed=True,
    Mode='retained', Result=dict(Kind='logical-row-retention-checkpoints', Rows=500,
    ReleaseGate=False, WholeApplicationMemoryBudget=False, Samples=phases))
assert len(module.validate_retention(retention, 500)) == 5
for case in ('descriptions', 'missing-probe', 'logical-payload', 'native-bytes',
             'unbounded-native', 'popup', 'removed-state', 'budget-claim'):
    bad = copy.deepcopy(retention)
    phase = bad['Result']['Samples'][1]
    if case == 'descriptions': phase['Descriptions'] -= 1
    elif case == 'missing-probe': del phase['Alive']['nativeControl']
    elif case == 'logical-payload': phase['LogicalPayloadBytes'] += 8192
    elif case == 'native-bytes': phase['NativeBytes'] += 32768
    elif case == 'unbounded-native':
        for key in ('nativeControl', 'nativePayload', 'popup'): phase['Alive'][key] = 500
        phase['NativeBytes'] = 500 * 32768
    elif case == 'popup': phase['Alive']['popup'] += 1
    elif case == 'removed-state': bad['Result']['Samples'][4]['Alive']['component'] = 1
    else: bad['Result']['WholeApplicationMemoryBudget'] = True
    try: module.validate_retention(bad, 500)
    except ValueError: pass
    else: raise AssertionError(case + ' must fail')
print('PASS: retention checkpoints enforce description, logical/native payload and removal accounting')
