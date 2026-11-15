"""Regression checks for deterministic skill generation and its freshness gate."""
import contextlib
import importlib.util
import io
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("skill_reference", ROOT / "tools/Update-SkillReference.py")
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


class SkillReferenceTests(unittest.TestCase):
    def test_every_shipped_signature_is_preserved_once(self):
        output = generator.api_reference(ROOT).splitlines()
        for project in ("UI Framework", "UI Framework.Wpf"):
            for line in (ROOT / project / "PublicAPI.Shipped.txt").read_text(encoding="utf-8-sig").splitlines():
                if line and not line.startswith("#"):
                    self.assertEqual(1, output.count(line), line)

    def test_exact_compiled_sources_are_embedded(self):
        output = generator.examples_reference(ROOT)
        for name in generator.EXAMPLES:
            source = (ROOT / f"tests/UI Framework.Checks/SkillExamples/{name}.cs").read_text(encoding="utf-8-sig").rstrip()
            self.assertIn("```csharp\n" + source + "\n```", output)

    def test_freshness_rejects_missing_and_modified_outputs(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            paths = ["Directory.Build.props", "UI Framework/PublicAPI.Shipped.txt", "UI Framework.Wpf/PublicAPI.Shipped.txt"]
            paths += [f"tests/UI Framework.Checks/SkillExamples/{name}.cs" for name in generator.EXAMPLES]
            for relative in paths:
                target = root / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(ROOT / relative, target)
            references = root / "skills/ui-framework/references"
            references.mkdir(parents=True)
            with patch.object(generator, "ROOT", root), contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                with patch.object(sys, "argv", ["generator", "--check"]):
                    with self.assertRaises(SystemExit) as failure:
                        generator.main()
                    self.assertEqual(1, failure.exception.code)
                with patch.object(sys, "argv", ["generator"]):
                    generator.main()
                with patch.object(sys, "argv", ["generator", "--check"]):
                    generator.main()
                    for name in ("api-surface.md", "behavior-examples.md"):
                        path = references / name
                        original = path.read_bytes()
                        path.write_bytes(original + b"stale\n")
                        with self.assertRaises(SystemExit) as failure:
                            generator.main()
                        self.assertEqual(1, failure.exception.code)
                        path.write_bytes(original)


if __name__ == "__main__":
    unittest.main()
