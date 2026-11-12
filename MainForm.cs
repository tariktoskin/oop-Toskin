using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PaintApp;

public class MainForm : Form
{
    private static readonly (string Name, string Title)[] Tools =
    {
        ("select", "Выбор"),
        ("line", "Линия"),
        ("rect", "Прямоугольник"),
        ("ellipse", "Эллипс"),
        ("pencil", "Карандаш"),
    };

    private readonly Drawing _drawing = new();
    private readonly CommandHistory _history = new();
    private readonly CanvasPanel _canvas = new();

    private readonly ToolStrip _toolStrip = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly List<ToolStripButton> _toolButtons = new();
    private readonly ToolStripButton _colorButton = new("Цвет");
    private readonly ToolStripComboBox _thicknessBox = new();
    private readonly ToolStripButton _undoButton = new("Отменить");
    private readonly ToolStripButton _redoButton = new("Повторить");

    private string _tool = "pencil";
    private Color _color = Color.Black;
    private int _thickness = 3;

    private Shape? _current;     // фигура, которую рисуют прямо сейчас (предпросмотр)
    private Shape? _selected;    // выделенная фигура
    private bool _moving;        // тянем выделенную фигуру
    private Point _lastPoint;

    public MainForm()
    {
        Text = "Графический редактор";
        ClientSize = new Size(900, 650);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterScreen;

        BuildInterface();
        SelectTool("pencil");
        AfterChange();
    }

    // ---------- Построение окна ----------

    private void BuildInterface()
    {
        // инструменты
        foreach (var (name, title) in Tools)
        {
            var button = new ToolStripButton(title)
            {
                Tag = name,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            button.Click += ToolButton_Click;
            _toolButtons.Add(button);
            _toolStrip.Items.Add(button);
        }
        _toolStrip.Items.Add(new ToolStripSeparator());

        // цвет
        _colorButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        _colorButton.Image = MakeSwatch(_color);
        _colorButton.Click += ColorButton_Click;
        _toolStrip.Items.Add(_colorButton);

        // толщина
        _toolStrip.Items.Add(new ToolStripLabel("Толщина:"));
        _thicknessBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _thicknessBox.Width = 50;
        _thicknessBox.Items.AddRange(new object[] { "1", "2", "3", "5", "8", "12" });
        _thicknessBox.SelectedIndex = 2;
        _thicknessBox.SelectedIndexChanged += (_, _) =>
            _thickness = int.Parse(_thicknessBox.SelectedItem?.ToString() ?? "3");
        _toolStrip.Items.Add(_thicknessBox);
        _toolStrip.Items.Add(new ToolStripSeparator());

        // отмена, повтор, очистка
        _undoButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _undoButton.Click += (_, _) => Undo();
        _redoButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _redoButton.Click += (_, _) => Redo();
        var clearButton = new ToolStripButton("Очистить") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        clearButton.Click += ClearButton_Click;
        _toolStrip.Items.Add(_undoButton);
        _toolStrip.Items.Add(_redoButton);
        _toolStrip.Items.Add(clearButton);
        _toolStrip.Items.Add(new ToolStripSeparator());

        // файлы
        var openButton = new ToolStripButton("Открыть") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        openButton.Click += OpenButton_Click;
        var saveButton = new ToolStripButton("Сохранить") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        saveButton.Click += SaveButton_Click;
        var exportButton = new ToolStripButton("В PNG") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        exportButton.Click += ExportButton_Click;
        _toolStrip.Items.Add(openButton);
        _toolStrip.Items.Add(saveButton);
        _toolStrip.Items.Add(exportButton);

        _toolStrip.Dock = DockStyle.Top;

        // строка состояния
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Dock = DockStyle.Bottom;

        // холст
        _canvas.Dock = DockStyle.Fill;
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;

        // сначала холст, потом панели по краям
        Controls.Add(_canvas);
        Controls.Add(_statusStrip);
        Controls.Add(_toolStrip);
    }

    private static Bitmap MakeSwatch(Color color)
    {
        var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(color);
        g.DrawRectangle(Pens.Gray, 0, 0, 15, 15);
        return bitmap;
    }

    // ---------- Мышь и рисование ----------

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        if (_tool == "select")
        {
            _selected = _drawing.HitTest(e.Location);
            _moving = _selected != null;
            _lastPoint = e.Location;
        }
        else
        {
            // фигуру создаёт фабрика по имени инструмента
            _current = ShapeFactory.Create(_tool, e.Location, _color, _thickness);
        }
        _canvas.Invalidate();
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_current != null)
        {
            _current.MoveEnd(e.Location);   // предпросмотр: тянем вторую точку
            _canvas.Invalidate();
        }
        else if (_moving && _selected != null)
        {
            _selected.Move(e.X - _lastPoint.X, e.Y - _lastPoint.Y);
            _lastPoint = e.Location;
            _canvas.Invalidate();
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_current != null)
        {
            var shape = _current;
            _current = null;
            if (!shape.IsDegenerate)
                Run(new AddShapeCommand(_drawing, shape));
            else
                _canvas.Invalidate();
        }
        _moving = false;
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        _drawing.Draw(g);
        _current?.Draw(g);

        if (_selected != null)
        {
            var box = _selected.Bounds;
            box.Inflate(6, 6);
            using var pen = new Pen(Color.DodgerBlue, 1) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, box);
        }
    }

    // ---------- Кнопки ----------

    private void ToolButton_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripButton { Tag: string name })
        {
            SelectTool(name);
            AfterChange();
        }
    }

    private void SelectTool(string name)
    {
        _tool = name;
        _current = null;
        _selected = null;
        _moving = false;

        foreach (var button in _toolButtons)
            button.Checked = (string?)button.Tag == name;   // подсветка выбранного
    }

    private void ColorButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog { Color = _color };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _color = dialog.Color;
        var old = _colorButton.Image;
        _colorButton.Image = MakeSwatch(_color);
        old?.Dispose();
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
        if (_drawing.Count > 0)
            Run(new ClearCommand(_drawing));
    }

    private void Undo()
    {
        _history.Undo();
        AfterChange();
    }

    private void Redo()
    {
        _history.Redo();
        AfterChange();
    }

    private void DeleteSelected()
    {
        if (_selected != null)
            Run(new RemoveShapeCommand(_drawing, _selected));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Z:
                Undo();
                return true;
            case Keys.Control | Keys.Y:
                Redo();
                return true;
            case Keys.Delete:
                DeleteSelected();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ---------- Файлы ----------

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Рисунок (*.json)|*.json",
            DefaultExt = "json",
            FileName = "рисунок.json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _drawing.Save(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Не удалось сохранить рисунок.\n" + ex.Message);
        }
    }

    private void OpenButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Рисунок (*.json)|*.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _drawing.Load(dialog.FileName);
            _history.Clear();
            _selected = null;
            AfterChange();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ShowError("Не удалось открыть рисунок.\n" + ex.Message);
        }
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Картинка (*.png)|*.png",
            DefaultExt = "png",
            FileName = "рисунок.png"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var bitmap = new Bitmap(_canvas.ClientSize.Width, _canvas.ClientSize.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                _drawing.Draw(g);
            }
            bitmap.Save(dialog.FileName, ImageFormat.Png);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException)
        {
            ShowError("Не удалось сохранить картинку.\n" + ex.Message);
        }
    }

    // ---------- Вспомогательное ----------

    // выполнить действие через историю (его можно будет отменить)
    private void Run(ICommand command)
    {
        _history.Run(command);
        AfterChange();
    }

    // после любого изменения: убрать выделение пропавшей фигуры, обновить кнопки и холст
    private void AfterChange()
    {
        if (_selected != null && !_drawing.Shapes.Contains(_selected))
            _selected = null;

        _undoButton.Enabled = _history.CanUndo;
        _redoButton.Enabled = _history.CanRedo;

        var toolTitle = Tools.First(t => t.Name == _tool).Title;
        _statusLabel.Text = $"Фигур: {_drawing.Count}    Инструмент: {toolTitle}";

        _canvas.Invalidate();
    }

    private void ShowError(string message) =>
        MessageBox.Show(this, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
