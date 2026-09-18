using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class EditorTests
{
    [TestMethod]
    public void MultilineEditorNormalizesOnceAndRetainsSelectionOnUnrelatedBuilds() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("one\ntwo");
        var revision = new State<int>(0);
        var writes = 0;
        var binding = new Binding<string>(() => text.Value, value => { writes++; text.Value = value.Trim(); });
        using var host = new ViewHost(() => VStack(Text(revision.Value.ToString()), TextEditor(binding).MaxLength(200).UndoLimit(5).Height(120)));
        TestVisualTree.Layout(host);
        var editor = TestVisualTree.Find<TextBox>(host).Single();
        Assert.IsTrue(editor.AcceptsReturn);
        Assert.AreEqual(TextWrapping.Wrap, editor.TextWrapping);
        Assert.AreEqual(ScrollBarVisibility.Auto, editor.VerticalScrollBarVisibility);
        Assert.AreEqual(200, editor.MaxLength);
        Assert.AreEqual(5, editor.UndoLimit);
        editor.Text = " edited\ntext ";
        TestVisualTree.Flush();
        Assert.AreEqual("edited\ntext", editor.Text);
        Assert.AreEqual(1, writes);
        editor.Select(2, 3);
        revision.Value++;
        TestVisualTree.Flush();
        Assert.AreSame(editor, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(2, editor.SelectionStart);
        Assert.AreEqual(3, editor.SelectionLength);
        text.Value = "external\nupdate";
        TestVisualTree.Flush();
        Assert.AreEqual(text.Value, editor.Text);
        Assert.AreEqual(1, writes);
    });

    [TestMethod]
    public void ReadOnlyTranscriptStaysSelectableDisablesUndoAndNeverWritesBack() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("transcript");
        var readOnly = new State<bool>(false);
        var writes = 0;
        using var host = new ViewHost(() => TextEditor(new Binding<string>(() => text.Value, value => { writes++; text.Value = value; }))
            .IsReadOnly(readOnly.Value).UndoLimit(4).MaxLength(3));
        TestVisualTree.Layout(host);
        var editor = TestVisualTree.Find<TextBox>(host).Single();
        readOnly.Value = true;
        TestVisualTree.Flush();
        Assert.IsTrue(editor.IsReadOnly);
        Assert.IsTrue(editor.IsEnabled);
        Assert.IsFalse(editor.IsUndoEnabled);
        Assert.IsFalse(editor.CanUndo);
        editor.Select(0, 5);
        Assert.AreEqual("trans", editor.SelectedText);
        editor.Text = "attempt";
        Assert.AreEqual("transcript", editor.Text);
        Assert.AreEqual(0, writes);
        text.Value = "updated transcript beyond native input limit";
        TestVisualTree.Flush();
        Assert.AreEqual(text.Value, editor.Text);
        readOnly.Value = false;
        TestVisualTree.Flush();
        Assert.IsTrue(editor.IsUndoEnabled);
        Assert.AreEqual(4, editor.UndoLimit);
    });

    [TestMethod]
    public void PasswordBindingRejectsEditsAndSwitchesSourcesWithoutFeedback() => StaTestRunner.Run(() =>
    {
        var left = new State<string>("first");
        var right = new State<string>("second");
        var useRight = new State<bool>(false);
        var writes = 0;
        var rejected = new Binding<string>(() => left.Value, _ => writes++);
        using var host = new ViewHost(() => PasswordField(useRight.Value ? right.Binding() : rejected).MaxLength(50));
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        var password = TestVisualTree.Find<PasswordBox>(host).Single();
        Assert.AreEqual("first", password.Password);
        Assert.AreEqual(50, password.MaxLength);
        Assert.IsNotNull(password.Template.FindName("PART_ContentHost", password));
        password.Password = "rejected";
        Assert.AreEqual("first", password.Password);
        Assert.AreEqual(1, writes);
        useRight.Value = true;
        TestVisualTree.Flush();
        Assert.AreSame(password, TestVisualTree.Find<PasswordBox>(host).Single());
        Assert.AreEqual("second", password.Password);
        password.Password = "accepted";
        Assert.AreEqual("accepted", right.Value);
        Assert.AreEqual("first", left.Value);
        right.Value = "external";
        TestVisualTree.Flush();
        Assert.AreEqual("external", password.Password);
        host.Dispose();
        password.Password = "after disposal";
        Assert.AreEqual("external", right.Value);
    });

    [TestMethod]
    public void PickerReadsBackRejectedValuesAndUpdatesOptionsWithoutWrites() => StaTestRunner.Run(() =>
    {
        var options = new State<string[]>(["same", "same", "third"]);
        var index = new State<int>(1);
        var reject = false;
        var writes = 0;
        var binding = new Binding<int>(() => index.Value, value => { writes++; if (!reject) index.Value = value; });
        using var host = new ViewHost(() => Picker(options.Value, binding));
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        var picker = TestVisualTree.Find<ComboBox>(host).Single();
        Assert.AreEqual(1, picker.SelectedIndex);
        picker.SelectedIndex = 2;
        Assert.AreEqual(2, index.Value);
        Assert.AreEqual(1, writes);
        reject = true;
        picker.SelectedIndex = 0;
        Assert.AreEqual(2, picker.SelectedIndex);
        Assert.AreEqual(2, writes);
        options.Value = ["only"];
        TestVisualTree.Flush();
        Assert.AreSame(picker, TestVisualTree.Find<ComboBox>(host).Single());
        Assert.AreEqual(-1, picker.SelectedIndex);
        Assert.AreEqual(2, index.Value);
        Assert.AreEqual(2, writes);
        options.Value = [];
        index.Value = -1;
        TestVisualTree.Flush();
        Assert.AreEqual(0, picker.Items.Count);
        options.Value = ["first", "second"];
        index.Value = 0;
        TestVisualTree.Flush();
        Assert.AreEqual(0, picker.SelectedIndex);
        Assert.AreEqual(2, writes);
    });

    [TestMethod]
    public void PickerUsesLatestBindingAndInheritsDisabledState() => StaTestRunner.Run(() =>
    {
        var left = new State<int>(0);
        var right = new State<int>(1);
        var useRight = new State<bool>(false);
        var enabled = new State<bool>(true);
        using var host = new ViewHost(() => Picker(["first", "second"], useRight.Value ? right : left).IsEnabled(enabled.Value));
        TestVisualTree.Layout(host);
        var picker = TestVisualTree.Find<ComboBox>(host).Single();
        useRight.Value = true;
        TestVisualTree.Flush();
        picker.SelectedIndex = -1;
        Assert.AreEqual(-1, right.Value);
        Assert.AreEqual(0, left.Value);
        enabled.Value = false;
        TestVisualTree.Flush();
        Assert.IsFalse(picker.IsEnabled);
        host.Dispose();
        picker.SelectedIndex = 0;
        Assert.AreEqual(-1, right.Value);
    });

    [TestMethod]
    public void TextModifiersRejectInvalidInputsAndPickerSnapshotsOptions()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TextEditor(new State<string>("")).MaxLength(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TextField(new State<string>("")).UndoLimit(-1));
        Assert.ThrowsException<InvalidOperationException>(() => PasswordField(new State<string>("")).IsReadOnly(true));
        var options = new[] { "before" };
        var view = Picker(options, new State<int>(0));
        options[0] = "after";
        Assert.AreEqual("before", view.Options[0]);
    }
}
