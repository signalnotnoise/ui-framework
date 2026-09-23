using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class EditorPropertyPrecedenceTests
{
    [TestMethod]
    public void InitialEditorValuesKeepPrecedenceWhenStyleChanges() => StaTestRunner.Run(() =>
    {
        var text = new State<string>("editable");
        using var host = new ViewHost(() => UI.TextEditor(text));
        TestVisualTree.Layout(host);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        var style = new Style(typeof(TextBox));
        style.Setters.Add(new Setter(TextBox.IsReadOnlyProperty, true));
        style.Setters.Add(new Setter(TextBox.MaxLengthProperty, 2));
        style.Setters.Add(new Setter(TextBox.IsUndoEnabledProperty, false));
        input.Style = style;
        Assert.IsFalse(input.IsReadOnly);
        Assert.AreEqual(0, input.MaxLength);
        Assert.IsTrue(input.IsUndoEnabled);
        input.Text = "still editable";
        Assert.AreEqual("still editable", text.Value);
        host.Refresh();
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
    });
}
