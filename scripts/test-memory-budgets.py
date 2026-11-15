"""Guard the distinction between bounded initialization, continuing growth and missing evidence."""
import importlib.util
import tempfile
import sys
import copy
import json
from pathlib import Path

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('budgets', Path(__file__).resolve().parents[1]/'tools/Test-MemoryBudgets.py')
budgets = importlib.util.module_from_spec(spec); spec.loader.exec_module(budgets)
limit = {'Heap': dict(peak=1000, growth=100, lateGrowth=20)}

# An initial bounded step that plateaus is distinct from ongoing late growth.
step = [{'Heap': 100 if i < 40 else 180} for i in range(180)]
assert not budgets.trends(step, limit, 32)['Heap']['exceeded']
linear = [{'Heap': 100+i} for i in range(180)]
assert 'lateGrowth' in budgets.trends(linear, limit, 32)['Heap']['exceeded']
spike = [dict(s) for s in step]; spike[80]['Heap'] = 1001
assert 'peak' in budgets.trends(spike, limit, 32)['Heap']['exceeded']
for invalid in ([{'Heap': 1}]*20, [{'Heap': float('nan')}]*180, [{'Heap': True}]*180):
    try: budgets.trends(invalid, limit, 32)
    except ValueError: pass
    else: raise AssertionError('Invalid or insufficient observations accepted')
with tempfile.TemporaryDirectory() as folder:
    policy = budgets.read(Path(__file__).resolve().parents[1]/'tools/memory-budgets.json')
    result = budgets.evaluate(folder, policy)
    assert not result['passed'] and result['evidenceErrors'] and not result['releaseGate']
    try: budgets.raw_matches(folder, [{'missing': True}])
    except ValueError: pass
    else: raise AssertionError('Missing raw samples accepted')
    # A browser's small heap must never override missing native process cleanup.
    samples = [dict(Cycle=i, Warmup=i < 4, Survivors=[], Collected={'ManagedBytes':100},
                    Evidence=dict(Runtime='test-runtime', AllCapturedProcessesExited=True,
                        RemainingProcesses=0, ParentReleases=1, ChildReleases=1, DeferralsCompleted=1,
                        BrowserPid=10, Processes=[dict(Id=10,PrivateMemorySize64=100),dict(Id=11,PrivateMemorySize64=100)])) for i in range(8)]
    report = dict(Passed=True,DispatcherShutdownFinished=True,CleanupPolicyClosed=True,Mode='retention-fresh',ReleaseGate=False,
                  Result=dict(Passed=True,Mode='retention-fresh',Cycles=8,Warmups=4,ReleaseGate=False,Samples=samples,
                    NegativeControlDetectedAndReleased=True,NegativeControl=dict(RetainedBeforeRelease=['parent-owner'],SurvivorsAfterRelease=[]),
                    ReportingRetention=dict(Protocol='bounded-sample-files-v1',Samples=8,VerifiedCollectedSamples=8,
                        MaximumPendingWeakProbes=1,SampleSurvivors=0,MeasurementBeforeReportReload=True)))
    raw = Path(folder)/'bounded-samples'; raw.mkdir()
    def write_samples(data):
        for i,s in enumerate(data['Result']['Samples']):
            (raw/f'{i:04}.json').write_text(json.dumps(s))
    write_samples(report)
    budgets.browser_samples(report,folder,'retention-fresh',8,policy['browser'])
    for field,value in [('RemainingProcesses',1),('ParentReleases',0),('AllCapturedProcessesExited',False)]:
        broken=copy.deepcopy(report); broken['Result']['Samples'][-1]['Evidence'][field]=value; write_samples(broken)
        try: budgets.browser_samples(broken,folder,'retention-fresh',8,policy['browser'])
        except ValueError: pass
        else: raise AssertionError('Browser cleanup failure accepted')
    oversized=copy.deepcopy(report)
    oversized['Result']['Samples'][-1]['Evidence']['Processes'][0]['PrivateMemorySize64']=policy['browser']['maximumLivePrivateBytes']+1
    write_samples(oversized)
    _, live=budgets.browser_samples(oversized,folder,'retention-fresh',8,policy['browser'])
    assert 'livePrivateBytes' in live['exceeded'], 'Child-process memory was omitted from the budget'
print('PASS: bounded startup distinguished from ongoing growth; peaks, missing evidence and invalid metrics rejected.')
print('PASS: browser process and one-time release failures cannot pass with a small heap.')
