// Exercise the shipped viewer's handlers with a small DOM fixture and its real
// embedded Three.js in no-WebGL mode. This is not a visual browser test.
const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
class Element {
  constructor() { this.children = []; this.style = {}; this.dataset = {}; this.value = ''; this.textContent = ''; this.checked = false; this.files = []; this.clientWidth = 800; this.clientHeight = 600; }
  append(...nodes) { for (const node of nodes) { this.children.push(node); node.parent = this; } }
  prepend(node) { this.children.unshift(node); }
  replaceChildren(...nodes) { this.children = []; this.textContent = ''; this.append(...nodes); }
  add(node) { this.append(node); }
  remove() { if (this.parent) this.parent.children = this.parent.children.filter(n => n !== this); }
  setAttribute(key, value) { this[key] = value; }
  addEventListener() {}
  getContext() { return null; }
}
const html = fs.readFileSync(process.argv[2], 'utf8');
const elements = new Map([...html.matchAll(/\bid="([^"]+)"/g)].map(match => [match[1], new Element()]));
for (const id of ['data', 'git-data']) elements.get(id).textContent = html.match(new RegExp(`<script id="${id}" type="application/json">([\\s\\S]*?)</script>`))[1];
elements.get('git-context').checked = true;
const context = vm.createContext({
  console: {log() {}, warn() {}, error() {}},
  document: {getElementById: id => elements.get(id), createElement: () => new Element(), createElementNS: () => new Element()},
  Option: class extends Element { constructor(label, value) { super(); this.textContent = label; this.value = value; } },
  devicePixelRatio: 1,
});
for (const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g)) vm.runInContext(match[1], context);
const evaluate = expression => vm.runInContext(expression, context);
const packet = JSON.parse(elements.get('git-data').textContent);
const originalPosition = evaluate('[positions.get("src/a.ts").x, positions.get("src/a.ts").y]').join(',');
assert(elements.get('git-status').textContent.includes(packet.basisRevision.slice(0, 12)));
assert.equal(elements.get('git-controls').hidden, false);
assert.equal(elements.get('timeline').children.length, packet.commits.length);
assert(elements.get('hint').textContent.includes('WebGL is unavailable'));
assert.equal(evaluate('historyMarks.nodes.get("src/b.ts")'), 'removed');
const removed = elements.get('git-files').children.find(e => e.textContent === 'D src/b.ts');
assert(removed, 'Deleted files must remain selectable');
removed.onclick();
assert.equal(evaluate('selected'), 'src/b.ts');
assert(elements.get('detail').children.some(e => e.textContent.includes('removed')));
elements.get('nodes').value = 'src/a.ts';
elements.get('nodes').onchange();
assert(elements.get('git-node-history').children.filter(e => e.onclick).length >= 2, 'Selecting a file should show both touching commits');
const older = packet.commits[1].commit;
elements.get('git-commit').value = older;
elements.get('git-commit').onchange();
assert.equal(elements.get('git-target').value, older);
assert.equal(elements.get('git-base').value, 'empty');
assert.equal(evaluate('[positions.get("src/a.ts").x, positions.get("src/a.ts").y]').join(','), originalPosition, 'Unchanged files keep their layout positions across revisions');
elements.get('git-base').value = older;
elements.get('git-compare').onclick();
assert.equal(evaluate('historyMarks.nodes.size'), 0);
assert.equal(evaluate('historyMarks.edges.size'), 0);
elements.get('git-current').onclick();
assert.equal(evaluate('historyMarks.active'), false);
assert(elements.get('git-summary').textContent.includes('Initial graph snapshot restored'));
elements.get('history-file').files = [{size: 1, text: async () => '{'}];
Promise.resolve(elements.get('history-file').onchange()).then(() => {
  assert(elements.get('git-status').textContent.startsWith('History load failed:'));
  console.log('Viewer handlers passed: history, commits, deletion, comparisons, current view, malformed import, no-WebGL fallback.');
}).catch(error => { console.error(error); process.exitCode = 1; });
