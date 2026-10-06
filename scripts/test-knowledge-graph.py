"""Exercise generation and the real hook in a disposable Git repository."""
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    with tempfile.TemporaryDirectory(prefix="ui-graph-") as directory:
        root = Path(directory)

        def run(*args, success=True):
            result = subprocess.run(args, cwd=root, capture_output=True, text=True)
            if success and result.returncode:
                raise AssertionError(result.stdout + result.stderr)
            if not success:
                assert result.returncode != 0, "Expected stale-output failure"
            return result.stdout

        def write(path, content):
            target = root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(content, encoding="utf-8", newline="\n")

        for path in ["scripts/update-knowledge-graph.py", "scripts/knowledge-graph.html",
                     "scripts/knowledge-graph-vendor/three.min.js", ".githooks/pre-commit"]:
            target = root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, target)
        os.chmod(root / ".githooks/pre-commit", 0o755)
        write("docs/knowledge-graph.json", json.dumps({"schemaVersion": 1, "nodes": [
            {"id": "core", "label": "Core", "status": "implemented", "summary": "Fixture",
             "sources": ["Core/Core.csproj"]}], "edges": []}))
        write("Core/Core.csproj", "<Project />")
        write("App/App.csproj", '<Project><ItemGroup><ProjectReference Include="../Core/Core.csproj" /></ItemGroup></Project>')
        write("README.md", "[Core](Core/Core.csproj)\n")
        run("git", "init", "-q")
        run("git", "config", "user.name", "Graph fixture")
        run("git", "config", "user.email", "fixture@example.invalid")
        run("git", "config", "core.hooksPath", ".githooks")
        run("git", "add", ".")
        command = (sys.executable, "scripts/update-knowledge-graph.py")
        run(*command)
        run(*command, "--check")
        outputs = [root / "docs/knowledge-graph" / name for name in ("graph.json", "graph.html")]
        original = [p.read_bytes() for p in outputs]
        run(*command)
        assert original == [p.read_bytes() for p in outputs]
        graph = json.loads(outputs[0].read_text())
        embedded = re.search(r'<script id="data" type="application/json">(.*?)</script>', outputs[1].read_text(), re.S)
        assert json.loads(embedded[1]) == graph
        ids = {n["id"] for n in graph["nodes"]}
        assert all(e["source"] in ids and e["target"] in ids for e in graph["edges"])
        assert any(e["kind"] == "project reference" for e in graph["edges"])
        assert any(e["kind"] == "evidenced by" for e in graph["edges"])
        assert not any("three.min.js" in identifier for identifier in ids)
        write("staged.md", "[Core](Core/Core.csproj)\n")
        run("git", "add", "staged.md")
        write("staged.md", "[App](App/App.csproj)\n")
        write("unstaged.md", "local-only\n")
        # Even edited renderer assets must be loaded from the index in commit mode.
        template = root / "scripts/knowledge-graph.html"
        template.write_text(template.read_text() + "<!-- UNSTAGED_ASSET -->")
        run(*command, "--check", success=False)
        run(*command)
        graph = json.loads(outputs[0].read_text())
        assert "unstaged.md" not in {n["id"] for n in graph["nodes"]}
        assert {"source": "staged.md", "target": "Core/Core.csproj", "kind": "references"} in graph["edges"]
        assert "UNSTAGED_ASSET" not in outputs[1].read_text()
        run(*command, "--worktree")
        run(*command, "--worktree", "--check")
        assert "UNSTAGED_ASSET" in outputs[1].read_text()
        # Commit only inside this fixture; the hook must replace preview outputs.
        run("git", "-c", "commit.gpgsign=false", "commit", "-qm", "Fixture")
        run(*command, "--check")
        assert run("git", "show", "HEAD:staged.md") == "[Core](Core/Core.csproj)\n"
        assert "unstaged.md" not in run("git", "ls-files").splitlines()
        assert "UNSTAGED_ASSET" not in run("git", "show", "HEAD:scripts/knowledge-graph.html")
        assert "docs/knowledge-graph/graph.json" in run("git", "ls-files").splitlines()
    print("PASS: staged isolation, hook scope, stale detection, deterministic output, embedded data and edges")


if __name__ == "__main__":
    main()
