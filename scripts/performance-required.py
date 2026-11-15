"""Check local eligibility for [skip performance] or [docs only] push markers."""
import argparse
from pathlib import Path
import re
import subprocess

# Deliberately narrow: new/unknown paths run the comparison until reviewed.
SAFE_FILES = {
    "README.md", "LICENSE", "CHANGELOG.md", "CONTRIBUTING.md",
    "docs/agent-skill.md", "docs/backlog.md", "docs/components.md",
    "docs/bindings.md", "docs/navigation.md", "docs/accessibility.md",
    "docs/package-readme.md", "docs/api-contract.md",
    "docs/knowledge-graph.json", "docs/knowledge-graph.md",
    "docs/knowledge-graph/graph.json", "docs/knowledge-graph/graph.html",
    "tools/Update-SkillReference.py", "scripts/test-skill-reference.py",
}


def safe_path(path, docs_only=False):
    if docs_only and (path.startswith(("tools/", "scripts/", "tests/"))):
        return False
    if path in SAFE_FILES:
        return True
    if path.startswith("skills/") and Path(path).suffix in {".md", ".yaml", ".json"}:
        return True
    if path.startswith("docs/images/") and Path(path).suffix in {".png", ".gif", ".jpg", ".svg"}:
        return True
    return path.startswith("tests/UI Framework.Checks/") and path.endswith(".cs")


def decide(base, head, git, staged=False, docs_only=False):
    if not all(isinstance(sha, str) and re.fullmatch(r"[0-9a-f]{40}", sha) and sha != "0" * 40 for sha in (base, head)):
        return True, "Missing or invalid revision range: full comparison required."
    try:
        # Preserve both sides of renames, including moves out of runtime paths.
        revisions = ["--cached", base] if staged else [base, head]
        output = git(["diff", "--name-only", "--no-renames", "-z", *revisions, "--"])
        paths = [path for path in output.split("\0") if path]
    except (OSError, subprocess.SubprocessError, UnicodeError):
        return True, "Revision comparison unavailable: full comparison required."
    if not paths:
        return True, "Empty comparison: full comparison required."
    if any(not safe_path(path, docs_only) for path in paths):
        return True, "Changed paths require validation; this marker is not eligible."
    category = "documentation/skill content" if docs_only else "non-runtime"
    return False, f"All {len(paths)} changed paths are explicitly classified as {category}."


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, help="Freshly fetched remote branch that will receive the push")
    parser.add_argument("--staged", action="store_true", help="Compare the proposed index against the remote, before committing")
    parser.add_argument("--docs-only", action="store_true", help="Check the stricter [docs only] marker; no code/tool/test edits")
    args = parser.parse_args()
    def git(arguments):
        return subprocess.check_output(["git", *arguments], encoding="utf-8", errors="strict").strip("\n")
    try:
        base = git(["rev-parse", "--verify", "--end-of-options", args.base + "^{commit}"])
        head = git(["rev-parse", "--verify", "HEAD"])
        required, reason = decide(base, head, git, args.staged, args.docs_only)
    except (OSError, subprocess.SubprocessError, UnicodeError):
        required, reason = True, "Local history unavailable: full comparison required."
    print(reason)
    marker = "[docs only]" if args.docs_only else "[skip performance]"
    if required:
        print(f"Do not add {marker} to the pushed tip commit.")
    else:
        print(f"Eligible for {marker} on the pushed tip commit. Recheck after any range/content change.")
    return 1 if required else 0


if __name__ == "__main__":
    raise SystemExit(main())
