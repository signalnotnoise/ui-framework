import json
from collections import defaultdict
from pathlib import Path

folder = Path(__file__).parent
trace = json.loads((folder / 'current.speedscope.json').read_text(encoding='utf-8-sig'))
names = [f['name'] for f in trace['shared']['frames']]
results = []
for profile in trace['profiles']:
    stack = []
    inclusive, exclusive = defaultdict(float), defaultdict(float)
    previous = profile['startValue']
    for event in profile['events']:
        elapsed = event['at'] - previous
        if stack:
            exclusive[stack[-1]] += elapsed
            for frame in set(stack):
                inclusive[frame] += elapsed
        if event['type'] == 'O':
            stack.append(event['frame'])
        else:
            assert stack.pop() == event['frame']
        previous = event['at']
    def top(values):
        return [{'frame': names[k], 'sampledMilliseconds': round(v, 3)}
                for k, v in sorted(values.items(), key=lambda p: -p[1])[:40]]
    results.append({'thread': profile['name'], 'inclusive': top(inclusive), 'exclusive': top(exclusive)})
(folder / 'per-thread.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
for result in results:
    print(result['thread'])
    for entry in result['exclusive'][:12]:
        print(entry)
