"""Deterministic file graph. Requires Python >=3.10 and Git."""
import argparse
import fnmatch
import hashlib
import json
import posixpath
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
OUTPUTS = ('docs/knowledge-graph/graph.json', 'docs/knowledge-graph/graph.html')
EXTENSIONS = {'.md', '.mdx', '.ts', '.tsx', '.js', '.jsx', '.mjs', '.cjs', '.cs', '.csproj', '.py', '.sh', '.go', '.rs', '.java', '.kt', '.rb', '.php', '.c', '.h', '.cpp', '.hpp', '.swift', '.vue', '.svelte', '.sql'}
EXTENSIONS |= {'.ps1', '.json', '.yml', '.yaml', '.props', '.targets', '.slnx', '.html'}
SPECIAL_FILES = {'.githooks/pre-commit', 'LICENSE'}
ARCHITECTURE = 'docs/knowledge-graph.json'
JS = {'.ts', '.tsx', '.js', '.jsx', '.mjs', '.cjs'}
CONFIG = '.knowledge-graph.json'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def revision(value):
    return git('rev-parse', '--verify', '--end-of-options', value + '^{commit}').decode().strip()


def read(path, worktree=False, rev=None):
    return (ROOT / path).read_bytes() if worktree else git('show', f'{rev or ""}:{path}')


def entries(worktree=False, rev=None):
    if rev:
        rows = git('ls-tree', '-rz', rev).split(b'\0')
        return {p.decode(): meta.split()[2].decode() for row in rows if row
                for meta, p in [row.split(b'\t', 1)] if meta.startswith(b'100')}
    result = {}
    for row in git('ls-files', '--stage', '-z').split(b'\0'):
        if not row:
            continue
        meta, path = row.split(b'\t', 1)
        mode, oid, stage = meta.split()
        if stage != b'0':
            raise SystemExit('Resolve index conflicts before graph generation.')
        if mode.startswith(b'100'):
            result[path.decode()] = oid.decode()
    if worktree:
        for p in git('ls-files', '--others', '--exclude-standard', '-z').decode().split('\0'):
            if p:
                result[p] = None
        result = {p: oid for p, oid in result.items() if (ROOT / p).is_file() and not (ROOT / p).is_symlink()}
    return result


def blobs(oids):
    """Read blobs in one process rather than one process per file."""
    unique = sorted(set(oids))
    raw = subprocess.check_output(['git', 'cat-file', '--batch'], input=('\n'.join(unique) + '\n').encode(), cwd=ROOT) if unique else b''
    offset, result = 0, {}
    for oid in unique:
        end = raw.index(b'\n', offset)
        size = int(raw[offset:end].split()[2])
        result[oid] = raw[end + 1:end + 1 + size]
        offset = end + size + 2
    return result


def references(source, suffix):
    if suffix in {'.md', '.mdx'}:
        pattern, kind = r'''\]\((<[^>\r\n]+>|[^\s)]+)(?:\s+(?:"[^"]*"|'[^']*'))?\s*\)''', 'links'
    elif suffix in JS:
        pattern, kind = r'(?:\bfrom\s+|\bimport\s*|\bimport\s*\(\s*)[\'\"]([^\'\"]+)[\'\"]', 'imports'
    elif suffix in {'.csproj', '.props', '.targets'}:
        # Retain this repository's explicit XML dependency adapter. No C# call
        # or using relationships are inferred. Comments keep their line count.
        ET.fromstring(source)
        source = re.sub(r'<!--.*?-->', lambda m: re.sub(r'[^\n]', ' ', m.group()), source, flags=re.S)
        pattern, kind = r'<(?:\w+:)?ProjectReference\b[^>]*?\bInclude\s*=\s*[\'\"]([^\'\"]+)[\'\"]', 'project reference'
    else:
        return []
    return [{'ref': m.group(1), 'line': source.count('\n', 0, m.start()) + 1,
             'text': m.group(0)[:240], 'kind': kind} for m in re.finditer(pattern, source)]


def build_graph(worktree=False, rev=None):
    all_entries = entries(worktree, rev)
    config = json.loads(read(CONFIG, worktree, rev)) if CONFIG in all_entries else {}
    extensions = set(config.get('extensions', sorted(EXTENSIONS)))
    paths = sorted(p for p in all_entries if p not in OUTPUTS
                   and not p.startswith('scripts/knowledge-graph-vendor/')
                   and (PurePosixPath(p).suffix in extensions or (extensions and p in SPECIAL_FILES))
                   and not any(fnmatch.fnmatchcase(p, x) for x in config.get('exclude', [])))
    fingerprint = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()[:16]
    cache_dir = Path(git('rev-parse', '--absolute-git-dir').decode().strip()) / 'knowledge-graph' / ('extraction-' + fingerprint)
    cached, missing = {}, []
    for p in paths:
        key = (all_entries[p] or '') + PurePosixPath(p).suffix
        try:
            cached[p] = json.loads((cache_dir / (key + '.json')).read_text()) if not worktree else None
        except (OSError, ValueError):
            cached[p] = None
        if cached[p] is None:
            missing.append(p)
    raw_blobs = blobs(all_entries[p] for p in missing) if not worktree else {}
    digests, extracted = [], {}
    for p in paths:
        if cached[p] is None:
            raw = read(p, True) if worktree else raw_blobs[all_entries[p]]
            refs = references(raw.decode('utf-8', errors='replace'), PurePosixPath(p).suffix)
            if not worktree:
                try:
                    cache_dir.mkdir(parents=True, exist_ok=True)
                    (cache_dir / (all_entries[p] + PurePosixPath(p).suffix + '.json')).write_text(json.dumps(refs), encoding='utf-8')
                except OSError:
                    pass
        else:
            refs = cached[p]
        extracted[p] = refs
        digests.append((p, hashlib.sha256(read(p, True)).hexdigest() if worktree else all_entries[p]))
    files = set(paths)
    nodes = [{'id': p, 'label': PurePosixPath(p).name, 'group': p.split('/')[0] if '/' in p else 'root', 'kind': 'file'} for p in paths]
    edges, unresolved = [], 0
    for p in paths:
        for evidence in extracted[p]:
            ref, kind = evidence['ref'], evidence['kind']
            if kind == 'imports' and not ref.startswith('.'):
                continue
            ref = unquote(ref.strip('<>').split('#')[0].split('?')[0])
            if not ref or ':' in ref or ref.startswith('/'):
                continue
            if kind == 'project reference':
                ref = ref.replace('\\', '/')
            target = posixpath.normpath(posixpath.join(posixpath.dirname(p), ref))
            candidates = [target]
            if kind == 'imports':
                if target.endswith('.js'):
                    candidates += [target[:-3] + ext for ext in ('.ts', '.tsx')]
                if not PurePosixPath(target).suffix:
                    candidates += [target + ext for ext in ('.ts', '.tsx', '.js', '.jsx', '.mjs')]
                    candidates += [target + '/index' + ext for ext in ('.ts', '.tsx', '.js', '.jsx')]
            resolved = next((x for x in candidates if x in files), None)
            if resolved and resolved != p:
                edges.append({'source': p, 'target': resolved, 'kind': kind,
                              'evidence': {'path': p, 'line': evidence['line'], 'text': evidence['text']}})
            elif not resolved:
                unresolved += 1
        group = p.split('/')[0] if '/' in p else 'root'
        edges.append({'source': 'group:' + group, 'target': p, 'kind': 'contains'})
    for group in sorted({n['group'] for n in nodes}):
        nodes.append({'id': 'group:' + group, 'label': group, 'group': group, 'kind': 'directory'})
    # Preserve the curated v1 architecture as an independent authority. Older
    # commits without it still reconstruct their file graph. This adapter must
    # not rewrite its schema or manufacture runtime/test relationships.
    concepts = set()
    if ARCHITECTURE in files:
        architecture = json.loads(read(ARCHITECTURE, worktree, rev))
        if architecture.get('schemaVersion') != 1:
            raise ValueError('Unsupported curated architecture schema')
        concepts = {n['id'] for n in architecture['nodes']}
        if len(concepts) != len(architecture['nodes']):
            raise ValueError('Duplicate architecture concept')
        for index, node in enumerate(architecture['nodes']):
            identifier = 'concept:' + node['id']
            nodes.append({**node, 'id': identifier, 'group': 'architecture', 'kind': 'concept'})
            for source_index, path in enumerate(node['sources']):
                if path not in files:
                    raise ValueError(f'Missing architecture evidence: {path}')
                edges.append({'source': identifier, 'target': path, 'kind': 'evidenced by',
                              'evidence': {'path': ARCHITECTURE, 'jsonPointer': f'/nodes/{index}/sources/{source_index}'}})
        for index, edge in enumerate(architecture['edges']):
            if edge['from'] not in concepts or edge['to'] not in concepts:
                raise ValueError('Unknown architecture edge endpoint')
            edges.append({'source': 'concept:' + edge['from'], 'target': 'concept:' + edge['to'],
                          'kind': edge['relation'], 'evidence': {'path': ARCHITECTURE, 'jsonPointer': f'/edges/{index}'}})
    return {'schemaVersion': 2, 'title': 'UI Framework knowledge graph', 'source': 'repository files and curated architecture',
            'inputDigest': hashlib.sha256(json.dumps([digests, config], sort_keys=True).encode()).hexdigest(),
            'coverage': {'unresolvedRelativeReferences': unresolved, 'semantic': False,
                         'curatedArchitectureConcepts': len(concepts),
                         'limitations': 'Regex syntax evidence may include comments/examples. C# has file nodes and explicit XML project references, not resolved using directives or calls. Curated concepts/relationships are imported from schema v1, not inferred. Aliases and runtime calls are not resolved.'},
            'nodes': nodes, 'edges': sorted(edges, key=lambda e: (e['source'], e['target'], e['kind'], e.get('evidence', {}).get('line', 0)))}


def render(graph, worktree=False, rev=None):
    template = read('scripts/knowledge-graph.html', worktree, rev).decode('utf-8').replace('\r\n', '\n')
    three = read('scripts/knowledge-graph-vendor/three.min.js', worktree, rev).decode('utf-8').replace('\r\n', '\n')
    three = re.sub(r'</script', r'<\/script', three, flags=re.IGNORECASE)
    data = json.dumps(graph, ensure_ascii=True).replace('<', '\\u003c')
    return template.replace('__THREE_JS__', three).replace('__GRAPH_JSON__', data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='compare with index outputs; disk outputs with --worktree')
    parser.add_argument('--worktree', action='store_true', help='preview tracked and nonignored untracked files')
    parser.add_argument('--protect', action='store_true', help='stop before overwriting unstaged or untracked outputs')
    args = parser.parse_args()
    if not args.worktree:
        own = Path(__file__).resolve().relative_to(ROOT).as_posix()
        if read(own).replace(b'\r\n', b'\n') != (ROOT / own).read_bytes().replace(b'\r\n', b'\n'):
            raise SystemExit('Extractor differs from index. Stage intended extractor changes or use --worktree.')
    graph = build_graph(args.worktree)
    outputs = dict(zip(OUTPUTS, (json.dumps(graph, indent=2) + '\n', render(graph, args.worktree))))
    if args.protect and not args.check:
        tracked = entries()
        for relative in OUTPUTS:
            if git('diff', '--', relative) or ((ROOT / relative).exists() and relative not in tracked):
                raise SystemExit(f'Preserve or discard unstaged/untracked generated output before committing: {relative}')
    for relative, content in outputs.items():
        if args.check:
            try:
                actual = read(relative, args.worktree).decode('utf-8').replace('\r\n', '\n')
            except (OSError, subprocess.CalledProcessError):
                actual = None
            if actual != content:
                raise SystemExit(f'{relative} is stale; regenerate in the same mode and stage outputs for index checks.')
        else:
            path = ROOT / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding='utf-8', newline='\n')
    print(f"Knowledge graph: {len(graph['nodes'])} nodes, {len(graph['edges'])} edges")


if __name__ == '__main__':
    main()
