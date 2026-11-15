"""Check local eligibility for a [skip performance] push commit marker."""
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


def safe_path(path):
    if path in SAFE_FILES:
        return True
    if path.startswith("skills/") and Path(path).suffix in {".md", ".yaml", ".json"}:
        return True
    if path.startswith("docs/images/") and Path(path).suffix in {".png", ".gif", ".jpg", ".svg"}:
        return True
    return path.startswith("tests/UI Framework.Checks/") and path.endswith(".cs")


def decide(base, head, git, staged=False):
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
    if any(not safe_path(path) for path in paths):
        return True, "Runtime, build, benchmark or unclassified paths changed."
    return False, f"All {len(paths)} changed paths are explicitly classified as non-runtime."


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, help="Freshly fetched remote branch that will receive the push")
    parser.add_argument("--staged", action="store_true", help="Compare the proposed index against the remote, before committing")
    args = parser.parse_args()
    def git(arguments):
        return subprocess.check_output(["git", *arguments], encoding="utf-8", errors="strict").strip("\n")
    try:
        base = git(["rev-parse", "--verify", "--end-of-options", args.base + "^{commit}"])
        head = git(["rev-parse", "--verify", "HEAD"])
        required, reason = decide(base, head, git, args.staged)
    except (OSError, subprocess.SubprocessError, UnicodeError):
        required, reason = True, "Local history unavailable: full comparison required."
    print(reason)
    if required:
        print("Do not add [skip performance] to the pushed tip commit.")
    else:
        print("Eligible for [skip performance] on the pushed tip commit. Recheck after any range/content change.")
    return 1 if required else 0


if __name__ == "__main__":
    raise SystemExit(main())
