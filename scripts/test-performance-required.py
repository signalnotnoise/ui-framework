import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("selection", Path(__file__).with_name("performance-required.py"))
selection = importlib.util.module_from_spec(spec)
spec.loader.exec_module(selection)


class PerformanceSelectionTests(unittest.TestCase):
    def decide(self, paths, base="a" * 40):
        return selection.decide(base, "b" * 40, lambda args: "\0".join(paths))[0]

    def test_known_non_runtime_changes_skip(self):
        self.assertFalse(self.decide(["docs/agent-skill.md", "skills/ui-framework/SKILL.md",
                                     "tests/UI Framework.Checks/SkillContractTests.cs", "docs/knowledge-graph/graph.json"]))

    def test_docs_only_excludes_code_even_when_performance_skip_is_allowed(self):
        for path in ["tests/UI Framework.Checks/SkillContractTests.cs", "tools/Update-SkillReference.py", "scripts/test-skill-reference.py"]:
            with self.subTest(path=path):
                self.assertFalse(selection.decide("a" * 40, "b" * 40, lambda args: path)[0])
                self.assertTrue(selection.decide("a" * 40, "b" * 40, lambda args: path, docs_only=True)[0])
        self.assertFalse(selection.decide("a" * 40, "b" * 40,
                         lambda args: "docs/agent-skill.md\0skills/ui-framework/SKILL.md\0docs/knowledge-graph/graph.json\0", docs_only=True)[0])

    def test_runtime_build_benchmark_policy_and_unknown_paths_run(self):
        for path in ["UI Framework/View.cs", "UI Framework.Wpf/ThemeStyles.cs", "samples/Counter/StressRunner.cs",
                     "Directory.Build.props", "global.json", ".github/workflows/validate.yml",
                     "tools/performance-baseline.json", "tools/Test-Performance.ps1", "docs/performance.md",
                     "docs/performance-guardrails.md", "docs/releasing.md", "AGENTS.md", "new-area/file.txt",
                     "tests/UI Framework.Checks/UI Framework.Checks.csproj", "scripts/performance-required.py"]:
            with self.subTest(path=path):
                self.assertTrue(self.decide(["docs/backlog.md", path]))

    def test_empty_and_missing_ranges_run(self):
        self.assertTrue(self.decide([]))
        for base in [None, "", "0" * 40, "--bad-option"]:
            self.assertTrue(self.decide(["docs/agent-skill.md"], base=base))

    def test_unavailable_git_range_runs(self):
        def fail(args):
            raise subprocess.CalledProcessError(128, "git")
        self.assertTrue(selection.decide("a" * 40, "b" * 40, fail)[0])

    def test_full_pushed_range_and_staged_tree_preserve_rename_sources(self):
        commands = []
        def git(args):
            commands.append(args)
            return "UI Framework/Removed.cs\0docs/Removed.md\0"
        self.assertTrue(selection.decide("a" * 40, "b" * 40, git)[0])
        self.assertEqual(["diff", "--name-only", "--no-renames", "-z", "a" * 40, "b" * 40, "--"], commands[0])
        selection.decide("a" * 40, "b" * 40, git, staged=True)
        self.assertEqual(["diff", "--name-only", "--no-renames", "-z", "--cached", "a" * 40, "--"], commands[1])

    def test_local_cli_checks_all_commits_and_index(self):
        script = Path(__file__).with_name("performance-required.py").resolve()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            def git(*args):
                return subprocess.check_output(["git", *args], cwd=root, text=True, stderr=subprocess.DEVNULL).strip()
            git("init")
            git("config", "user.name", "Selection Test")
            git("config", "user.email", "selection@example.invalid")
            (root / "README.md").write_text("base")
            git("add", ".")
            git("commit", "-m", "base")
            base = git("rev-parse", "HEAD")
            def check(*args):
                return subprocess.run([sys.executable, str(script), "--base", base, *args], cwd=root, capture_output=True).returncode
            (root / "README.md").write_text("docs")
            git("add", ".")
            self.assertEqual(0, check("--staged"))
            self.assertEqual(0, check("--staged", "--docs-only"))
            git("commit", "-m", "docs")
            self.assertEqual(0, check())
            (root / "Runtime.cs").write_text("// unknown runtime")
            git("add", ".")
            self.assertEqual(1, check("--staged"))
            self.assertEqual(1, check("--staged", "--docs-only"))
            git("commit", "-m", "runtime")
            (root / "README.md").write_text("more docs")
            git("add", ".")
            git("commit", "-m", "docs at tip")
            self.assertEqual(1, check())
            self.assertEqual(1, check("--docs-only"))


if __name__ == "__main__":
    unittest.main()
