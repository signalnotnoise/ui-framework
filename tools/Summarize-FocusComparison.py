"""Validate all scheduled focus/window cases before comparing process resources."""
import json
import sys
from pathlib import Path


def summarize(root):
    root = Path(root)
    audit = json.loads((root / 'app-audit.json').read_text(encoding='utf-8-sig'))
    expected = ['Plain-quiet', 'Framework-quiet', 'Plain-focus', 'Framework-focus',
                'Framework-focus', 'Plain-focus', 'Plain-focus', 'Framework-focus']
    assert [a['phase'] for a in audit] == [f'{i}-{case}-{phase}' for i, case in enumerate(expected) for phase in ('start', 'exit')]
    assert all(not a['apps'] for a in audit), 'GuidedGrade detected'
    runs = []
    for i, case in enumerate(expected):
        path = root / f'{i}-{case}'
        result = json.loads((path / 'result.json').read_text())
        focus = case.endswith('-focus')
        assert result['Kind'] == ('plain-wpf' if case.startswith('Plain') else 'framework-native')
        assert result['Passed'] and result['Error'] is None and result['Survivors'] == 0
        assert result['Windows'] == 30 and result['FocusRoundsPerWindow'] == 3
        assert result['Focus'] == focus and result['VerifiedFocus'] == (90 if focus else 0)
        assert result['ReleaseGate'] is False
        if not focus:
            assert result['Activations'] == 0
        if case.startswith('Plain'):
            assert not any(a.startswith('UI Framework') for a in result['Assemblies'])
        samples = [json.loads(p.read_text()) for p in sorted(path.glob('[0-9]*.json'))]
        expected_phases = ['process-start', 'anchor-unfocused', 'anchor-ready']
        for cycle in range(30):
            expected_phases += ['window-open', 'focus-round', 'focus-round', 'focus-round', 'cycle-closed']
        expected_phases += ['windows-closed', 'dispatcher-exit', 'thread-exit']
        assert [s['Phase'] for s in samples] == expected_phases
        closed = [s for s in samples if s['Phase'] == 'cycle-closed']
        assert [s['Cycle'] for s in closed] == list(range(30))
        assert all(s['Alive'] == 0 for s in closed)
        assert [s['VerifiedFocus'] for s in closed] == [3 * (i+1) if focus else 0 for i in range(30)]
        runs.append(dict(case=path.name, runtime=result['Runtime'], snapshots=len(samples),
                         windows=30, focusAcquisitions=result['VerifiedFocus'],
                         checkpoints={s['Phase']: s['Resources'] for s in samples if s['Cycle'] == -1},
                         firstClosed=closed[0]['Resources'], lastClosed=closed[-1]['Resources'],
                         finalTenRanges={k: [min(s['Resources'][k] for s in closed[-10:]),
                                             max(s['Resources'][k] for s in closed[-10:])]
                                         for k in ('Gdi', 'User', 'Handles', 'Heap')}))
    assert len({r['runtime'] for r in runs}) == 1
    return dict(releaseGate=False, memoryBudgetApproved=False, windows=240,
                focusAcquisitions=540, snapshots=1248, runs=runs)


if __name__ == '__main__':
    summary = summarize(sys.argv[1])
    Path(sys.argv[2]).write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
    for r in summary['runs']:
        print(r['case'], 'closed GDI', r['firstClosed']['Gdi'], '->', r['lastClosed']['Gdi'],
              'final ten', r['finalTenRanges']['Gdi'], 'thread-exit', r['checkpoints']['thread-exit'])
