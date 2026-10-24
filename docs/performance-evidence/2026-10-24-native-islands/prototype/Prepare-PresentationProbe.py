from pathlib import Path
import shutil
root=Path.cwd()
target=root/"artifacts/fw003/presentation-probe"
shutil.copytree(root/"artifacts/fw003/repeat/candidate-source",target)
p=target/"samples/Counter/StressRunner.cs"
s=p.read_text().replace("using System.Windows;", "using System.Windows;\nusing System.Windows.Interop;",1)
s=s.replace("        using var host = new ViewHost(() => Component<Dashboard>(screen => screen.Model = model));", """        using var presentation = args.Contains("--presentation-source")
            ? new HwndSource(new HwndSourceParameters("Presentation source probe") {
                Width = 1320, Height = 1080, PositionX = -10000, PositionY = -10000,
                WindowStyle = unchecked((int)0x80000000) }) : null;
        using var host = new ViewHost(() => Component<Dashboard>(screen => screen.Model = model));
        if (presentation is not null) presentation.RootVisual = host;""",1)
s=s.replace("        host.Dispose();", "        if (presentation is not null) presentation.RootVisual = null;\n        host.Dispose();",1)
p.write_text(s)
print(target)
