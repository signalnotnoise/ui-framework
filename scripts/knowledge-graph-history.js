/* Offline Git model. No network calls, HTML evaluation, or embedded executable commit text. */
globalThis.GraphHistory = (() => {
  const edgeKey = edge => edge._historyKey || JSON.stringify([edge.source, edge.target, edge.kind]);
  function indexedEdges(values) {
    const counts = new Map(), result = new Map();
    for (const edge of values) {
      const key = JSON.stringify([edge.source, edge.target, edge.kind]);
      const ordinal = counts.get(key) || 0; counts.set(key, ordinal + 1);
      result.set(JSON.stringify([edge.source, edge.target, edge.kind, ordinal]), edge);
    }
    return result;
  }
  function validate(packet) {
    if (!packet || packet.kind !== 'repository-graph-history-export' || packet.schemaVersion !== 1 ||
        typeof packet.basisRevision !== 'string' || !['worktree','commit'].includes(packet.viewMode) ||
        !Array.isArray(packet.commits) || !packet.commits.length || packet.commits.length > 20 || !packet.frames ||
        Object.keys(packet.frames).length > 41) throw new Error('Unsupported or oversized Git history bundle.');
    for (const frame of Object.values(packet.frames)) {
      if (!Array.isArray(frame.nodes) || !Array.isArray(frame.edges) || !frame.blobs ||
          frame.nodes.length > 30000 || frame.edges.length > 150000) throw new Error('Invalid or oversized revision graph.');
      if (frame.nodes.some(n => typeof n.id !== 'string' || typeof n.label !== 'string' || typeof n.group !== 'string') ||
          frame.edges.some(e => typeof e.source !== 'string' || typeof e.target !== 'string' || typeof e.kind !== 'string')) throw new Error('Invalid graph metadata.');
      const ids = new Set(frame.nodes.map(n => n.id));
      if (ids.size !== frame.nodes.length || frame.edges.some(e => !ids.has(e.source) || !ids.has(e.target)))
        throw new Error('Revision graph has duplicate nodes or missing endpoints.');
    }
    for (const c of packet.commits) {
      if (typeof c.commit !== 'string' || typeof c.parent !== 'string' || typeof c.subject !== 'string' || typeof c.date !== 'string' ||
          !Array.isArray(c.changes) || c.changes.some(change => typeof change.path !== 'string' || typeof change.status !== 'string' || (change.oldPath != null && typeof change.oldPath !== 'string')) ||
          !Object.hasOwn(packet.frames, c.commit) || !Object.hasOwn(packet.frames, c.parent))
        throw new Error('Commit is missing its revision graphs.');
    }
    return packet;
  }
  function compare(before, after) {
    if (before.unavailable || after.unavailable) throw new Error('This revision is unavailable locally. Choose two available revisions; no history was fetched.');
    const oldNodes = new Map(before.nodes.map(n => [n.id, n])), newNodes = new Map(after.nodes.map(n => [n.id, n]));
    const oldEdges = indexedEdges(before.edges), newEdges = indexedEdges(after.edges);
    const nodes = new Map(), edges = new Map();
    for (const [id, node] of oldNodes) if (!newNodes.has(id)) nodes.set(id, 'removed');
    for (const [id, node] of newNodes) {
      if (!oldNodes.has(id)) nodes.set(id, 'added');
      else if (JSON.stringify(oldNodes.get(id)) !== JSON.stringify(node) ||
               (before.blobs[id] !== after.blobs[id])) nodes.set(id, 'modified');
    }
    for (const [id] of oldEdges) if (!newEdges.has(id)) edges.set(id, 'removed');
    for (const [id, edge] of newEdges) {
      if (!oldEdges.has(id)) edges.set(id, 'added');
      else if (JSON.stringify(oldEdges.get(id)) !== JSON.stringify(edge)) edges.set(id, 'modified');
    }
    const unionEdges = [...newEdges, ...[...oldEdges].filter(([id]) => !newEdges.has(id))].map(([id, edge]) => ({...edge, _historyKey: id}));
    const affected = new Set(nodes.keys());
    for (const e of unionEdges) {
      if (e.kind !== 'contains' && (nodes.has(e.source) || nodes.has(e.target) || edges.has(edgeKey(e)))) {
        affected.add(e.source); affected.add(e.target);
      }
    }
    return {active: true, nodes, edges, affected,
      graph: {nodes: [...newNodes.values(), ...[...oldNodes].filter(([id]) => !newNodes.has(id)).map(([, n]) => n)],
              edges: unionEdges, coverage: after.coverage}};
  }
  function fileHistory(packet, paths) {
    const aliases = new Set(paths), result = [];
    for (const commit of packet.commits) {
      const matches = commit.changes.filter(change => aliases.has(change.path) || aliases.has(change.oldPath));
      if (!matches.length) continue;
      result.push({...commit, matches});
      for (const change of matches) if (change.status.startsWith('R') && aliases.has(change.path) && change.oldPath) aliases.add(change.oldPath);
    }
    return result;
  }
  return {validate, compare, fileHistory, edgeKey};
})();
