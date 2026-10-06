"""Render a deterministic repository graph from Git's index (the next commit).

Only staged, tracked files are read, so secrets in local configuration and
unstaged work never become graph content. Uses Python's standard library.
"""
import argparse
import json
import re
import subprocess
import posixpath
import xml.etree.ElementTree as ET
from urllib.parse import unquote
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[1]
OUTPUTS = {"docs/knowledge-graph/graph.json", "docs/knowledge-graph/graph.html"}
EXTENSIONS = {".md", ".mdx", ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs", ".cs", ".csproj", ".py", ".sh", ".go", ".rs", ".java", ".kt", ".rb", ".php", ".c", ".h", ".cpp", ".hpp", ".swift", ".vue", ".svelte", ".sql"}
EXTENSIONS |= {".ps1", ".json", ".yml", ".yaml", ".props", ".targets", ".slnx", ".html"}


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def build_graph(worktree=False):
    def read(path):
        return ((ROOT / path).read_bytes() if worktree else git("show", f":{path}")).decode("utf-8-sig")

    args = ("ls-files", "--cached", "--others", "--exclude-standard", "-z") if worktree else ("ls-files", "--cached", "-z")
    paths = sorted({p for p in git(*args).decode().split("\0")
                    if p and p not in OUTPUTS and not p.startswith("scripts/knowledge-graph-vendor/") and (PurePosixPath(p).suffix in EXTENSIONS or p in {".githooks/pre-commit", "LICENSE"})
                    and (not worktree or (ROOT / p).is_file())})
    files = set(paths)
    nodes = [{"id": p, "label": PurePosixPath(p).name,
              "group": p.split("/")[0] if "/" in p else "root"} for p in paths]
    edges = set()
    for path in paths:
        source = read(path)
        # Links in runbooks and relative ES module imports describe dependencies.
        refs = re.findall(r"\]\(([^)]+)\)", source)
        refs += re.findall(r'(?:from\s+|import\s*)[\'"]([^\'"]+)[\'"]', source)
        for ref in refs:
            ref = unquote(ref.split("#")[0].strip("<>"))
            if not ref or ":" in ref or ref.startswith("/"):
                continue
            parts = list(PurePosixPath(path).parent.parts)
            for part in ref.split("/"):
                if part == "..":
                    if parts:
                        parts.pop()
                elif part and part != ".":
                    parts.append(part)
            target = "/".join(parts)
            # TypeScript imports use .js extensions for the emitted Node modules.
            if target not in files and target.endswith(".js"):
                target = target[:-3] + ".ts"
            if target in files and target != path:
                edges.add((path, target, "references"))
        # Directory grouping makes the repository topology visible even for
        # files that have no explicit imports or Markdown links.
        group = path.split("/")[0] if "/" in path else "root"
        edges.add(("group:" + group, path, "contains"))
        if path.endswith((".csproj", ".props", ".targets")):
            for element in ET.fromstring(source).iter():
                if element.tag.split("}")[-1] == "ProjectReference" and element.get("Include"):
                    target = posixpath.normpath(posixpath.join(posixpath.dirname(path), element.get("Include").replace("\\", "/")))
                    if target in files:
                        edges.add((path, target, "project reference"))
    for group in sorted({node["group"] for node in nodes}):
        nodes.append({"id": "group:" + group, "label": group, "group": group})
    # Preserve the curated schema as the authority for architectural claims.
    architecture = json.loads(read("docs/knowledge-graph.json"))
    if architecture.get("schemaVersion") != 1:
        raise ValueError("Unsupported curated architecture schema")
    concepts = {node["id"] for node in architecture["nodes"]}
    if len(concepts) != len(architecture["nodes"]):
        raise ValueError("Duplicate architecture concept")
    for node in architecture["nodes"]:
        identifier = "concept:" + node["id"]
        nodes.append({**node, "id": identifier, "group": "architecture"})
        for source in node["sources"]:
            if source not in files:
                raise ValueError(f"Missing architecture evidence: {source}")
            edges.add((identifier, source, "evidenced by"))
    for edge in architecture["edges"]:
        if edge["from"] not in concepts or edge["to"] not in concepts:
            raise ValueError("Unknown architecture edge endpoint")
        edges.add(("concept:" + edge["from"], "concept:" + edge["to"], edge["relation"]))
    return {"schemaVersion": 1, "title": "UI Framework knowledge graph", "source": "repository files and curated architecture",
            "nodes": nodes,
            "edges": [{"source": a, "target": b, "kind": k} for a, b, k in sorted(edges)]}


def render(graph, worktree=False):
    def read_asset(relative):
        raw = (ROOT / relative).read_bytes() if worktree else git("show", f":{relative}")
        return raw.decode("utf-8").replace("\r\n", "\n")

    template = read_asset("scripts/knowledge-graph.html")
    three = read_asset("scripts/knowledge-graph-vendor/three.min.js")
    # Prevent JavaScript source from terminating its inline script element.
    three = re.sub(r"</script", r"<\/script", three, flags=re.IGNORECASE)
    # The embedded copy allows opening the HTML directly without an HTTP server.
    data = json.dumps(graph, ensure_ascii=True).replace("<", "\\u003c")
    return template.replace("__THREE_JS__", three).replace("__GRAPH_JSON__", data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="fail if committed outputs are stale")
    parser.add_argument("--worktree", action="store_true", help="preview current files, including unstaged changes")
    args = parser.parse_args()
    graph = build_graph(args.worktree)
    outputs = {"docs/knowledge-graph/graph.json": json.dumps(graph, indent=2) + "\n",
               "docs/knowledge-graph/graph.html": render(graph, args.worktree)}
    for relative, content in outputs.items():
        path = ROOT / relative
        if args.check:
            if not path.exists() or path.read_text(encoding="utf-8") != content:
                raise SystemExit(f"{relative} is stale; run python scripts/update-knowledge-graph.py")
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding="utf-8", newline="\n")
    print(f"Knowledge graph: {len(graph['nodes'])} nodes, {len(graph['edges'])} edges")


if __name__ == "__main__":
    main()
