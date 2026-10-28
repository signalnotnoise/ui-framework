"""Behavior tests in isolated temporary repositories. Run with Python 3.10+."""
import json
import importlib.util
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'scripts'


class GraphTests(unittest.TestCase):
    def export_history(self, *args, ok=True):
        return self.run_cmd(sys.executable, 'scripts/export-knowledge-graph-history.py', *args, **{'ok': ok})

    def test_offline_history_export_and_root_commit(self):
        result = json.loads(self.export_history('--limit', '1').stdout)
        packet = json.loads(Path(result['history']).read_text())
        self.assertEqual(packet['basisRevision'], self.first)
        self.assertIn('empty', packet['frames'])
        self.assertFalse(packet['historyTruncated'])
        self.assertEqual(packet['commits'][0]['parent'], 'empty')
        html = Path(result['html']).read_text()
        self.assertIn('id="git-base"', html)
        self.assertIn('id="git-node-history"', html)
        embedded = json.loads(re.search(r'<script id="git-data" type="application/json">(.*?)</script>', html).group(1))
        self.assertEqual(embedded, packet)
        original = Path(result['html']).read_bytes()
        self.export_history('--limit', '1', '--force')
        self.assertEqual(original, Path(result['html']).read_bytes())
        self.assertNotEqual(self.export_history('--limit', '1', ok=False).returncode, 0)
        self.assertNotEqual(self.export_history('--output', 'README.md', '--force', ok=False).returncode, 0)
        self.assertNotEqual(self.export_history('--output', 'scripts/knowledge-graph.html', '--force', ok=False).returncode, 0)

    def test_history_rename_deletion_safe_messages_and_worktree_isolation(self):
        self.git('mv', 'src/a.ts', 'src/renamed file.ts')
        self.git('rm', 'src/b.ts')
        self.git('commit', '-qm', '</script><img src=x onerror=alert(1)>')
        target = self.git('rev-parse', 'HEAD').stdout.strip()
        self.write('src/local.ts', 'export const local = true;')
        staged = self.git('diff', '--cached', '--name-only').stdout
        result = json.loads(self.export_history('--limit', '1', '--worktree').stdout)
        packet = json.loads(Path(result['history']).read_text())
        commit = packet['commits'][0]
        self.assertEqual(commit['commit'], target)
        self.assertTrue(packet['historyTruncated'])
        self.assertTrue(any(c['status'].startswith('R') and c['oldPath'] == 'src/a.ts' and c['path'] == 'src/renamed file.ts' for c in commit['changes']))
        self.assertTrue(any(c['status'] == 'D' and c['path'] == 'src/b.ts' for c in commit['changes']))
        before = packet['frames'][self.first]
        after = packet['frames'][target]
        self.assertIn('src/b.ts', {n['id'] for n in before['nodes']})
        self.assertNotIn('src/b.ts', {n['id'] for n in after['nodes']})
        self.assertNotIn('src/local.ts', after['blobs'])
        html = Path(result['html']).read_text()
        self.assertNotIn('</script><img src=x', html)
        graph = json.loads(re.search(r'<script id="data" type="application/json">(.*?)</script>', html).group(1))
        self.assertIn('src/local.ts', {n['id'] for n in graph['nodes']})
        self.assertEqual(staged, self.git('diff', '--cached', '--name-only').stdout)

    def test_history_merge_first_parent_and_shallow_boundary(self):
        original = self.git('symbolic-ref', '--short', 'HEAD').stdout.strip()
        self.git('checkout', '-qb', 'side')
        self.write('src/side.ts', 'export const side = true;')
        self.git('add', 'src/side.ts')
        self.git('commit', '-qm', 'side commit')
        self.git('checkout', '-q', original)
        self.git('merge', '--no-ff', '-qm', 'merge side', 'side')
        result = json.loads(self.export_history('--limit', '1').stdout)
        packet = json.loads(Path(result['history']).read_text())
        self.assertEqual(packet['commits'][0]['parent'], self.first)
        self.assertTrue(any(c['path'] == 'src/side.ts' for c in packet['commits'][0]['changes']))
        clone = self.root.parent / (self.root.name + '-shallow')
        self.addCleanup(lambda: shutil.rmtree(clone, ignore_errors=True))
        self.run_cmd('git', 'clone', '-q', '--depth', '1', self.root.as_uri(), str(clone))
        p = subprocess.run([sys.executable, 'scripts/export-knowledge-graph-history.py', '--limit', '1'], cwd=clone, capture_output=True, text=True, timeout=60)
        self.assertEqual(p.returncode, 0, p.stderr)
        shallow = json.loads(Path(json.loads(p.stdout)['history']).read_text())
        self.assertTrue(shallow['shallow'])
        self.assertTrue(shallow['frames'][self.first]['unavailable'])
        self.assertNotEqual(shallow['commits'][0]['parent'], 'empty')

    def test_history_detects_file_mode_changes(self):
        self.git('update-index', '--chmod=+x', 'src/a.ts')
        self.git('commit', '-qm', 'change executable mode only')
        result = json.loads(self.export_history('--limit', '1').stdout)
        packet = json.loads(Path(result['history']).read_text())
        target = packet['basisRevision']
        self.assertNotEqual(packet['frames'][target]['blobs']['src/a.ts'], packet['frames'][self.first]['blobs']['src/a.ts'])

    def test_template_like_paths_remain_inert_data(self):
        path = 'src/__HISTORY_JS__-__GIT_HISTORY_JSON__.ts'
        self.write(path, 'export const harmless = true;')
        self.git('add', path)
        self.generate()
        html = (self.root / 'docs/knowledge-graph/graph.html').read_text()
        embedded = json.loads(re.search(r'<script id="data" type="application/json">(.*?)</script>', html).group(1))
        self.assertIn(path, {node['id'] for node in embedded['nodes']})

    @unittest.skipUnless(shutil.which('node'), 'Node.js optional for browser-model checks')
    def test_history_viewer_handlers_without_webgl(self):
        self.git('rm', 'src/b.ts')
        self.write('src/a.ts', 'export const a = 2;')
        self.git('add', 'src/a.ts')
        self.git('commit', '-qm', 'modify a and delete b')
        result = json.loads(self.export_history('--limit', '2').stdout)
        self.run_cmd(shutil.which('node'), str(ASSETS / 'test-knowledge-graph-viewer.cjs'), result['html'])

    @unittest.skipUnless(shutil.which('node'), 'Node.js optional for browser-model checks')
    def test_history_javascript_model_and_viewer_syntax(self):
        js = ASSETS / 'knowledge-graph-history.js'
        code = r'''
const vm=require('node:vm'),fs=require('node:fs'),assert=require('node:assert/strict');
vm.runInThisContext(fs.readFileSync(process.argv[1],'utf8'));
const frame=(ids,edges=[],blobs={})=>({nodes:ids.map(id=>({id,kind:'file'})),edges,blobs});
const old=frame(['old.ts','gone.ts'],[{source:'old.ts',target:'gone.ts',kind:'imports'}],{'old.ts':'1'});
const next=frame(['new.ts'],[],{'new.ts':'2'});
const diff=GraphHistory.compare(old,next);
assert.equal(diff.nodes.get('old.ts'),'removed');assert.equal(diff.nodes.get('new.ts'),'added');
assert.equal([...diff.edges.values()][0],'removed');assert(diff.graph.nodes.some(n=>n.id==='gone.ts'));
assert.equal(GraphHistory.compare(frame(['a'],[],{a:'1'}),frame(['a'],[],{a:'2'})).nodes.get('a'),'modified');
const e={source:'a',target:'b',kind:'links',evidence:{line:1}};
assert.equal([...GraphHistory.compare(frame(['a','b'],[e]),frame(['a','b'],[{...e,evidence:{line:2}}])).edges.values()][0],'modified');
const duplicates=GraphHistory.compare(frame(['a','b'],[e,{...e,evidence:{line:3}}]),frame(['a','b'],[e]));
assert.equal(duplicates.graph.edges.length,2);assert.equal([...duplicates.edges.values()].filter(value=>value==='removed').length,1);
assert.throws(()=>GraphHistory.compare({...old,unavailable:true},next),/unavailable/);
const packet={commits:[{commit:'new',changes:[{path:'new.ts',oldPath:'old.ts',status:'R100'}]},{commit:'old',changes:[{path:'old.ts',status:'M'}]}]};
assert.deepEqual(GraphHistory.fileHistory(packet,['new.ts']).map(c=>c.commit),['new','old']);
assert.throws(()=>GraphHistory.validate({}),/Unsupported/);
const html=fs.readFileSync(process.argv[2],'utf8');
for(const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g)){if(!match[1].includes('__'))new vm.Script(match[1]);}
'''
        self.run_cmd(shutil.which('node'), '-e', code, str(js), str(ASSETS / 'knowledge-graph.html'))

    def test_cache_identity_is_not_authority(self):
        self.query('snapshot')
        cache = next((self.root/'.git/knowledge-graph').glob('revisions-*/*.json'))
        for value in ([], {'revision': 'wrong', 'snapshotMethod': 'reconstructed-with-current-extractor'}):
            cache.write_text(json.dumps(value))
            result = json.loads(self.query('snapshot').stdout)
            self.assertEqual(result['revision'], self.first)
            self.assertEqual(result['snapshotMethod'], 'reconstructed-with-current-extractor')

    def test_impact_shares_caps_across_revisions(self):
        self.write('src/a.ts', 'import "./b"; // modified')
        self.git('add', 'src/a.ts')
        self.git('commit', '-qm', 'modify for impact')
        result = json.loads(self.query('impact', '--base', self.first, '--limit', '1', '--max-edges', '1').stdout)
        self.assertLessEqual(len(result['before']['nodes']) + len(result['after']['nodes']), 1)
        self.assertLessEqual(len(result['before']['edges']) + len(result['after']['edges']), 1)

    def test_curated_architecture_and_project_evidence_adapter(self):
        self.write('Core/Core.csproj', '<Project />')
        self.write('App/App.csproj', '<Project>\n<ItemGroup><ProjectReference Include="../Core/Core.csproj" /></ItemGroup></Project>')
        architecture = {'schemaVersion': 1, 'nodes': [
            {'id': 'core', 'label': 'Core owner', 'status': 'implemented', 'summary': 'Human-authored boundary',
             'sources': ['Core/Core.csproj']}], 'edges': []}
        self.write('docs/knowledge-graph.json', json.dumps(architecture, indent=2))
        self.git('add', 'Core', 'App', 'docs/knowledge-graph.json')
        self.generate()
        graph = self.graph()
        concept = next(node for node in graph['nodes'] if node['id'] == 'concept:core')
        self.assertEqual(concept['summary'], 'Human-authored boundary')
        self.assertEqual(concept['kind'], 'concept')
        self.assertEqual(json.loads((self.root/'docs/knowledge-graph.json').read_text()), architecture)
        edge = next(edge for edge in graph['edges'] if edge['kind'] == 'project reference')
        self.assertEqual(edge['target'], 'Core/Core.csproj')
        self.assertEqual(edge['evidence']['line'], 2)
        self.assertTrue(any(edge['kind'] == 'evidenced by' for edge in graph['edges']))
        self.assertEqual(graph['coverage']['curatedArchitectureConcepts'], 1)

    def test_angle_link_spaces_and_type_specific_extraction(self):
        self.write('UI Framework/View.cs', 'namespace Framework; // import "./missing";')
        self.write('README.md', '[View](<UI Framework/View.cs> "view description")\n')
        self.git('add', 'UI Framework', 'README.md')
        self.generate()
        self.assertTrue(any(edge['target'] == 'UI Framework/View.cs' and edge['kind'] == 'links' for edge in self.graph()['edges']))
        self.assertFalse(any(edge['kind'] == 'imports' and edge['source'].endswith('.cs') for edge in self.graph()['edges']))

    def test_unstaged_renderer_isolation(self):
        path = self.root/'scripts/knowledge-graph.html'
        path.write_text(path.read_text() + '\n<!-- local viewer edit -->\n')
        self.generate()
        self.assertNotIn('local viewer edit', (self.root/'docs/knowledge-graph/graph.html').read_text())
        self.generate('--worktree')
        self.assertIn('local viewer edit', (self.root/'docs/knowledge-graph/graph.html').read_text())

    def test_protect_untracked_outputs(self):
        self.git('rm', '--cached', 'docs/knowledge-graph/graph.json')
        original = (self.root/'docs/knowledge-graph/graph.json').read_bytes()
        self.assertNotEqual(self.generate('--protect', ok=False).returncode, 0)
        self.assertEqual((self.root/'docs/knowledge-graph/graph.json').read_bytes(), original)

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix='graph-fixture-')
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.git('init', '-q')
        self.git('config', 'user.name', 'Graph Fixture')
        self.git('config', 'user.email', 'fixture@example.invalid')
        self.git('config', 'core.autocrlf', 'false')
        self.git('config', 'core.hooksPath', '.no-hooks')
        scripts = self.root / 'scripts'
        scripts.mkdir()
        for name in ('update-knowledge-graph.py', 'query-knowledge-graph.py', 'knowledge-graph.html', 'knowledge-graph-agents.md', 'knowledge-graph-history.js', 'export-knowledge-graph-history.py'):
            shutil.copyfile(ASSETS / name, scripts / name)
        vendor = scripts / 'knowledge-graph-vendor'
        vendor.mkdir()
        shutil.copyfile(ASSETS / 'knowledge-graph-vendor/three.min.js', vendor / 'three.min.js')
        self.write('src/a.ts', 'import {b} from "./b";\n')
        self.write('src/b.ts', 'export const b = 1;\n')
        self.write('README.md', '[a](src/a.ts)\n')
        self.write('.gitignore', 'artifacts/\n')
        self.git('add', '.')
        self.generate()
        self.git('add', 'docs')
        self.git('commit', '-qm', 'initial')
        self.first = self.git('rev-parse', 'HEAD').stdout.strip()

    def run_cmd(self, *args, ok=True):
        p = subprocess.run(args, cwd=self.root, capture_output=True, text=True, encoding='utf-8', timeout=60)
        if ok:
            self.assertEqual(p.returncode, 0, p.stdout + p.stderr)
        return p

    def git(self, *args, **kw):
        return self.run_cmd('git', *args, **kw)

    def generate(self, *args, **kw):
        return self.run_cmd(sys.executable, 'scripts/update-knowledge-graph.py', *args, **kw)

    def query(self, *args, **kw):
        return self.run_cmd(sys.executable, 'scripts/query-knowledge-graph.py', *args, **kw)

    def write(self, path, text):
        p = self.root / path
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding='utf-8', newline='\n')

    def graph(self):
        return json.loads((self.root / 'docs/knowledge-graph/graph.json').read_text())

    def test_deterministic_embedded_data_and_evidence(self):
        first = (self.root / 'docs/knowledge-graph/graph.html').read_bytes()
        self.generate()
        self.assertEqual(first, (self.root / 'docs/knowledge-graph/graph.html').read_bytes())
        self.generate('--check')
        graph = self.graph()
        html = first.decode()
        embedded = re.search(r'<script id="data" type="application/json">(.*?)</script>', html, re.S).group(1)
        self.assertEqual(graph, json.loads(embedded))
        ids = {n['id'] for n in graph['nodes']}
        self.assertTrue(all(e['source'] in ids and e['target'] in ids for e in graph['edges']))
        edge = next(e for e in graph['edges'] if e['source'] == 'src/a.ts')
        self.assertEqual((edge['kind'], edge['target'], edge['evidence']['line']), ('imports', 'src/b.ts', 1))

    def test_index_and_worktree_separation(self):
        self.write('src/staged.ts', 'export const staged = true;')
        self.git('add', 'src/staged.ts')
        self.write('src/local.ts', 'export const local = true;')
        self.write('src/a.ts', 'import "./local";')
        self.generate()
        ids = {n['id'] for n in self.graph()['nodes']}
        self.assertIn('src/staged.ts', ids)
        self.assertNotIn('src/local.ts', ids)
        self.assertTrue(any(e['source'] == 'src/a.ts' and e['target'] == 'src/b.ts' for e in self.graph()['edges']))
        self.assertNotEqual(self.generate('--check', ok=False).returncode, 0)
        self.git('add', 'docs')
        self.generate('--check')
        self.generate('--worktree')
        self.generate('--worktree', '--check')
        self.assertIn('src/local.ts', {n['id'] for n in self.graph()['nodes']})

    def test_output_protection(self):
        target = self.root / 'docs/knowledge-graph/graph.json'
        target.write_text('manual edit')
        self.assertNotEqual(self.generate('--protect', ok=False).returncode, 0)
        self.assertEqual(target.read_text(), 'manual edit')
        target.unlink()
        self.assertNotEqual(self.generate('--protect', ok=False).returncode, 0)
        self.assertFalse(target.exists())

    def test_unstaged_extractor_rejected(self):
        path = self.root / 'scripts/update-knowledge-graph.py'
        path.write_text(path.read_text() + '\n# unstaged edit\n')
        self.assertNotEqual(self.generate(ok=False).returncode, 0)
        self.generate('--worktree')

    def test_renderer_and_vendor_staleness(self):
        for relative in ('scripts/knowledge-graph.html', 'scripts/knowledge-graph-vendor/three.min.js'):
            path = self.root / relative
            path.write_text(path.read_text() + '\n/* changed */\n')
            self.git('add', relative)
            self.assertNotEqual(self.generate('--check', ok=False).returncode, 0)
            self.generate()
            self.git('add', 'docs')
            self.generate('--check')

    def test_history_rename_and_old_revision(self):
        self.git('mv', 'src/b.ts', 'src/renamed.ts')
        self.git('commit', '-qm', 'rename without regenerating')
        history = json.loads(self.query('history', 'src/renamed.ts').stdout)
        self.assertEqual(len(history['items']), 2)
        old = json.loads(self.query('locate', 'src/b.ts', '--at', self.first).stdout)
        self.assertEqual(old['items'][0]['id'], 'src/b.ts')
        current = json.loads(self.query('locate', 'src/renamed.ts').stdout)
        self.assertEqual(current['items'][0]['id'], 'src/renamed.ts')
        self.assertEqual(json.loads(self.query('snapshot').stdout)['snapshotMethod'], 'reconstructed-with-current-extractor')

    def test_deleted_file_impact(self):
        self.git('rm', 'src/b.ts')
        self.git('commit', '-qm', 'delete dependency')
        impact = json.loads(self.query('impact', '--base', self.first).stdout)
        self.assertIn('src/a.ts', {n['id'] for n in impact['before']['nodes']})
        self.assertNotIn('src/b.ts', {n['id'] for n in impact['after']['nodes']})

    def test_bounded_queries_and_pagination(self):
        result = json.loads(self.query('locate', '', '--limit', '1').stdout)
        self.assertEqual(len(result['items']), 1)
        self.assertTrue(result['truncated'])
        self.assertEqual(result['nextOffset'], 1)
        next_page = json.loads(self.query('locate', '', '--limit', '1', '--offset', '1').stdout)
        self.assertNotEqual(result['items'], next_page['items'])
        neighbors = json.loads(self.query('neighbors', 'src/a.ts', '--limit', '1').stdout)
        self.assertEqual(len(neighbors['nodes']), 1)
        self.assertTrue(neighbors['truncated'])
        self.assertNotEqual(self.query('locate', '', '--limit', '201', ok=False).returncode, 0)
        small = self.query('locate', '', '--max-chars', '1000', ok=False)
        self.assertEqual(small.returncode, 2)
        self.assertLess(len(small.stdout), 1000)

    def test_agents_preserved_and_idempotent(self):
        self.write('AGENTS.md', '# Existing rules\n\nKeep these instructions.\n')
        self.query('install-agents')
        first = (self.root / 'AGENTS.md').read_text()
        self.assertTrue(first.startswith('# Existing rules\n\nKeep these instructions.'))
        self.query('install-agents')
        self.assertEqual(first, (self.root / 'AGENTS.md').read_text())
        self.write('AGENTS.md', '<!-- knowledge-graph:start -->\nIncomplete')
        self.assertNotEqual(self.query('install-agents', ok=False).returncode, 0)
        self.assertIn('Incomplete', (self.root / 'AGENTS.md').read_text())

    def test_unicode_exclusions_and_html_escaping(self):
        self.write('src/caf\u00e9 file.ts', 'export const n = 1;')
        self.write('README.md', '[unicode](src/caf%C3%A9%20file.ts)\n')
        self.write('src/injection.ts', 'import "./missing</script>";')
        self.write('ignored/local.ts', 'export const secret = true;')
        self.write('.knowledge-graph.json', '{"exclude": ["ignored/*"]}')
        self.git('add', '.')
        self.generate()
        ids = {n['id'] for n in self.graph()['nodes']}
        self.assertIn('src/caf\u00e9 file.ts', ids)
        self.assertNotIn('ignored/local.ts', ids)
        self.assertTrue(any(e['target'] == 'src/caf\u00e9 file.ts' for e in self.graph()['edges']))
        spec = importlib.util.spec_from_file_location('fixture_builder', self.root / 'scripts/update-knowledge-graph.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        payload = {'nodes': [{'id': '</script><script>alert(1)</script>'}], 'edges': []}
        rendered = module.render(payload, worktree=True)
        embedded = re.search(r'<script id="data" type="application/json">(.*?)</script>', rendered, re.S).group(1)
        self.assertNotIn('<script>', embedded)
        self.assertEqual(json.loads(embedded), payload)

    def test_cache_rebuild_and_empty_repository(self):
        self.query('snapshot')
        cache = self.root / '.git/knowledge-graph'
        shutil.rmtree(cache)
        self.generate('--check')
        self.write('.knowledge-graph.json', '{"extensions": []}')
        self.git('add', '.knowledge-graph.json')
        self.generate()
        self.assertEqual(self.graph()['nodes'], [])
        self.assertEqual(self.graph()['edges'], [])

    def test_hook_stages_only_outputs(self):
        hook = self.root / '.githooks/pre-commit'
        hook.parent.mkdir()
        hook.write_bytes((ROOT / '.githooks/pre-commit').read_bytes().replace(b'\r\n', b'\n'))
        self.git('config', 'core.hooksPath', '.githooks')
        self.write('src/b.ts', 'export const b = 2;')
        self.git('add', 'src/b.ts')
        self.write('src/local.ts', 'unstaged')
        self.git('commit', '-qm', 'hook commit')
        changed = self.git('diff-tree', '--no-commit-id', '--name-only', '-r', 'HEAD').stdout.splitlines()
        self.assertEqual(set(changed), {'src/b.ts', 'docs/knowledge-graph/graph.json', 'docs/knowledge-graph/graph.html'})
        self.assertIn('?? src/local.ts', self.git('status', '--porcelain').stdout)
        self.generate('--check')


if __name__ == '__main__':
    unittest.main(verbosity=2)
