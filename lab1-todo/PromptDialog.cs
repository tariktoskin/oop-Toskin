namespace TodoApp;

// окно с одним полем ввода, для переименования
public static class PromptDialog
{
    public static string? Ask(IWin32Window owner, string caption, string label, string initial)
    {
        using var form = new Form
        {
            Text = caption,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(380, 110)
        };

        var text = new Label { Text = label, Left = 12, Top = 12, AutoSize = true };
        var box = new TextBox { Left = 12, Top = 36, Width = 356, Text = initial };
        var ok = new Button { Text = "OK", Left = 212, Top = 72, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Отмена", Left = 293, Top = 72, Width = 75, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { text, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Shown += (_, _) => box.SelectAll();

        return form.ShowDialog(owner) == DialogResult.OK ? box.Text : null;
    }
}
