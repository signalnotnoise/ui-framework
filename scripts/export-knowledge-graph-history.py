"""Export bounded local Git history into a standalone offline graph viewer."""
import argparse
import importlib.util
import json
import subprocess
from pathlib import Path
import sys

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('graph_query', Path(__file__).with_name('query-knowledge-graph.py'))
q = importlib.util.module_from_spec(spec)
spec.loader.exec_module(q)
g = q.g
KIND = 'repository-graph-history-export'
MAX_BYTES = 64 * 1024 * 1024


def file_identities(oid, files):
    result = {}
    for row in g.git('ls-tree', '-rz', oid).split(b'\0'):
        if not row:
            continue
        metadata, path = row.split(b'\t', 1)
        mode, kind, blob = metadata.split()
        path = path.decode('utf-8', errors='replace')
        if path in files:
            result[path] = mode.decode() + ':' + blob.decode()
    return result


def changes(parent, commit):
    if parent:
        args = ['diff', '--no-ext-diff', '--no-textconv', '--name-status', '-z', '-M', parent, commit, '--']
    else:
        args = ['diff-tree', '--root', '--no-commit-id', '--no-ext-diff', '--no-textconv', '--name-status', '-r', '-z', '-M', commit, '--']
    fields = g.git(*args).decode('utf-8', errors='replace').split('\0')
    result, i = [], 0
    while i < len(fields) and fields[i]:
        status, path = fields[i:i + 2]
        i += 2
        old = path
        if status.startswith(('R', 'C')):
            path = fields[i]
            i += 1
        result.append({'status': status, 'path': path, 'oldPath': old if old != path else None})
    return result


def export(limit=8, worktree=False):
    head = g.revision('HEAD')
    rows = g.git('log', '--first-parent', f'--max-count={limit + 1}', '--format=%H%x00%P%x00%aI%x00%s', head).decode('utf-8', errors='replace').splitlines()
    commits, frames = [], {}
    for row in rows[:limit]:
        oid, parents, date, subject = row.split('\0', 3)
        # Read real parents: git log hides boundary parents in shallow clones.
        header = g.git('cat-file', '-p', oid).decode('utf-8', errors='replace').split('\n\n', 1)[0]
        parents = ' '.join(line[7:] for line in header.splitlines() if line.startswith('parent '))
        parent = parents.split()[0] if parents else 'empty'
        parent_available = parent == 'empty' or subprocess.run(['git', 'cat-file', '-e', parent + '^{commit}'], cwd=g.ROOT, capture_output=True).returncode == 0
        commits.append({'commit': oid, 'parent': parent, 'date': date, 'subject': subject[:500],
                        'changes': changes(None if parent == 'empty' else parent, oid) if parent_available else [],
                        'changesUnavailable': not parent_available})
    for oid in dict.fromkeys(value for c in commits for value in (c['commit'], c['parent'])):
        print('Reading graph snapshot ' + oid[:12], file=sys.stderr)
        if oid == 'empty':
            frames[oid] = {'nodes': [], 'edges': [], 'blobs': {}, 'label': 'Empty tree (root commit)'}
            continue
        available = subprocess.run(['git', 'cat-file', '-e', oid + '^{commit}'], cwd=g.ROOT, capture_output=True).returncode == 0
        if not available:
            frames[oid] = {'nodes': [], 'edges': [], 'blobs': {}, 'unavailable': True,
                           'label': 'Parent unavailable in local history (no fetch performed)'}
            continue
        snapshot = q.snapshot(oid)
        file_ids = {n['id'] for n in snapshot['nodes'] if n.get('kind') == 'file'}
        label = next((c['subject'] for c in commits if c['commit'] == oid), 'Parent of oldest exported commit')
        frames[oid] = {'nodes': snapshot['nodes'], 'edges': snapshot['edges'],
                       'blobs': file_identities(oid, file_ids),
                       'label': label, 'coverage': snapshot['coverage']}
    return {'kind': KIND, 'schemaVersion': 1, 'basisRevision': head,
            'viewMode': 'worktree' if worktree else 'commit',
            'workingTreeDirty': bool(g.git('status', '--porcelain', '--untracked-files=normal').strip()),
            'shallow': g.git('rev-parse', '--is-shallow-repository').decode().strip() == 'true',
            'historyTruncated': len(rows) > limit, 'limit': limit, 'commits': commits, 'frames': frames,
            'note': 'Bounded first-parent history; merge changes compare with the first parent. Renames are Git heuristics. Snapshots reconstructed with the current extractor. No network fetch.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--limit', type=q.bounded(1, 20), default=8, help='recent first-parent commits (1..20)')
    parser.add_argument('--worktree', action='store_true', help='include current uncommitted files in the initial graph view')
    parser.add_argument('--output', default='artifacts/knowledge-graph-history/graph.html')
    parser.add_argument('--force', action='store_true', help='replace an existing export at the requested output paths')
    args = parser.parse_args()
    html = (g.ROOT / args.output).resolve()
    packet_path = html.with_suffix('.history.json')
    if html.suffix.lower() != '.html':
        raise SystemExit('Choose an .html output path.')
    tracked = set(g.entries())
    for path in (html, packet_path):
        if path.is_relative_to(g.ROOT) and path.relative_to(g.ROOT).as_posix() in tracked:
            raise SystemExit('History exports must not replace tracked files.')
        if path.is_relative_to(g.ROOT) and subprocess.run(['git', 'check-ignore', '-q', '--', str(path.relative_to(g.ROOT))], cwd=g.ROOT).returncode != 0:
            raise SystemExit('Choose an ignored artifact directory or a path outside the repository, so exports cannot become graph inputs.')
        if path.exists() and not args.force:
            raise SystemExit(f'Use a new output path or --force: {path}')
    packet = export(args.limit, args.worktree)
    if any(len(frame['nodes']) > 30000 or len(frame['edges']) > 150000 for frame in packet['frames'].values()):
        raise SystemExit('A revision exceeds viewer capacity (30,000 nodes / 150,000 edges). Use bounded CLI queries; no partial export was written.')
    raw = json.dumps(packet, ensure_ascii=True, separators=(',', ':'))
    if len(raw.encode()) > MAX_BYTES:
        raise SystemExit('History bundle exceeds 64 MiB. Use a smaller --limit; no partial export was written.')
    graph = g.build_graph(worktree=True) if args.worktree else q.snapshot(packet['basisRevision'])
    # Use the current installed renderer, even when committed snapshots predate these views.
    content = g.render(graph, worktree=True, history=packet)
    html.parent.mkdir(parents=True, exist_ok=True)
    html.write_text(content, encoding='utf-8', newline='\n')
    packet_path.write_text(raw + '\n', encoding='utf-8', newline='\n')
    print(json.dumps({'html': str(html), 'history': str(packet_path), 'commits': len(packet['commits']),
                      'basisRevision': packet['basisRevision'], 'historyTruncated': packet['historyTruncated'],
                      'shallow': packet['shallow'], 'historyBytes': len(raw.encode())}))


if __name__ == '__main__':
    main()
