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
