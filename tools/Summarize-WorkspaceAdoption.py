"""Verify package provenance and summarize diagnostic workspace upgrade measurements."""
import json
import statistics
import sys
from pathlib import Path

root = Path(sys.argv[1])
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
campaign = read(root/'campaign.json')
manifests = read(root/'assembly-manifests.json')
hashes = {side: {e['name']: e['sha256'].upper() for e in entries} for side, entries in manifests.items()}
libraries = {'UI Framework.dll', 'UI Framework.Wpf.dll'}
assert hashes['binary-upgrade'].keys() == hashes['baseline'].keys()
for file, value in hashes['binary-upgrade'].items():
    assert value == hashes['candidate' if file in libraries else 'baseline'][file], file
summary = dict(passed=True, releaseGate=False, campaign=campaign, modes={},
               interpretation='Three alternating process pairs; first cycle excluded. Diagnostic evidence, not a hosted release gate or general memory budget.')
for mode in ('full-list', 'virtualized'):
    reports = {}
    for side in ('baseline', 'candidate', 'binary-upgrade'):
        paths = [root/f'{mode}-binary-upgrade/report.json'] if side == 'binary-upgrade' else [root/f'{mode}-{i}-{side}/report.json' for i in range(campaign['repetitions'])]
        reports[side] = [read(p) for p in paths]
        for report in reports[side]:
            assert report['passed'] and report['survivors'] == 0 and report['cleanupClosed'] and report['dispatcherShutdown']
            assert report['rows'] == 100 and report['updatesPerCycle'] == 100 and report['cycles'] == 4
            assert report['virtualized'] == (mode == 'virtualized') and not report['releaseGate']
            assert len(report['records']) == 4
            for i, record in enumerate(report['records']):
                assert record['cycle'] == i and record['warmup'] == (i == 0)
                assert record['Creates'] == record['Releases'] == 2 and record['Mounts'] == record['Unmounts'] == 2
                assert record['updateBuilds'] == 100
            for assembly in report['assemblies']:
                assert assembly['sha256'].upper() == hashes[side][assembly['name']+'.dll']
    metrics = {}
    for metric in ('mountMs', 'updateMs', 'mountBytes', 'updateBytes', 'updateBuilds'):
        values = {side: [statistics.median(r[metric] for r in report['records'] if not r['warmup']) for report in reports[side]] for side in ('baseline','candidate')}
        medians = {side: statistics.median(v) for side,v in values.items()}
        metrics[metric] = dict(processMedians=values, medians=medians, changePercent=(medians['candidate']/medians['baseline']-1)*100)
    memory = {side: {metric: [min(r[metric] for r in rs), max(r[metric] for r in rs)] for metric in ('collectedManagedBytes','PrivateMemorySize64','HandleCount')} for side,rs in reports.items()}
    summary['modes'][mode] = dict(metrics=metrics, postShutdownRanges=memory)
output = root/'summary.json'
if output.exists(): raise SystemExit('Use a fresh summary path; preserve prior evidence.')
output.write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary,indent=2))
