using System.Globalization;

namespace TodoApp;

public class MainForm : Form
{
    private static readonly string DataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TodoApp", "todos.json");

    private readonly TodoList _list = new();

    private readonly TextBox _titleBox = new();
    private readonly TextBox _dueBox = new();
    private readonly Button _addButton = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _filterBox = new();
    private readonly CheckBox _sortCheck = new();
    private readonly ListView _listView = new();
    private readonly Button _renameButton = new();
    private readonly Button _deleteButton = new();
    private readonly Label _statusLabel = new();

    private readonly Font _doneFont;
    private bool _refreshing;   // true, пока список перерисовывается (чтобы не реагировать на галочки)

    public MainForm()
    {
        Text = "Список дел";
        ClientSize = new Size(640, 480);
        MinimumSize = new Size(520, 360);
        StartPosition = FormStartPosition.CenterScreen;

        _doneFont = new Font(Font, FontStyle.Strikeout);

        BuildInterface();
        LoadTasks();
        RefreshList();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _doneFont.Dispose();
        base.Dispose(disposing);
    }

    // ---------- Построение окна ----------

    private void BuildInterface()
    {
        // Ряд 1: название, срок, кнопка «Добавить»
        _titleBox.Dock = DockStyle.Fill;
        _titleBox.PlaceholderText = "Название новой задачи";
        _titleBox.KeyDown += InputBox_KeyDown;

        _dueBox.Dock = DockStyle.Fill;
        _dueBox.PlaceholderText = "Срок дд.мм.гггг";
        _dueBox.KeyDown += InputBox_KeyDown;

        _addButton.Text = "Добавить";
        _addButton.Dock = DockStyle.Fill;
        _addButton.Click += AddButton_Click;

        var addRow = CreateRow(3, new ColumnStyle(SizeType.Percent, 100),
                                  new ColumnStyle(SizeType.Absolute, 130),
                                  new ColumnStyle(SizeType.Absolute, 100));
        addRow.Controls.Add(_titleBox, 0, 0);
        addRow.Controls.Add(_dueBox, 1, 0);
        addRow.Controls.Add(_addButton, 2, 0);

        // Ряд 2: поиск, фильтр, сортировка
        _searchBox.Dock = DockStyle.Fill;
        _searchBox.PlaceholderText = "Поиск по названию";

        _filterBox.Dock = DockStyle.Fill;
        _filterBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _filterBox.Items.AddRange(new object[] { "Все", "Активные", "Выполненные" });
        _filterBox.SelectedIndex = 0;

        _sortCheck.Text = "По сроку";
        _sortCheck.AutoSize = true;
        _sortCheck.Anchor = AnchorStyles.Left;

        var filterRow = CreateRow(3, new ColumnStyle(SizeType.Percent, 100),
                                     new ColumnStyle(SizeType.Absolute, 130),
                                     new ColumnStyle(SizeType.AutoSize));
        filterRow.Controls.Add(_searchBox, 0, 0);
        filterRow.Controls.Add(_filterBox, 1, 0);
        filterRow.Controls.Add(_sortCheck, 2, 0);

        // Список задач
        _listView.Dock = DockStyle.Fill;
        _listView.View = View.Details;
        _listView.CheckBoxes = true;
        _listView.FullRowSelect = true;
        _listView.GridLines = true;
        _listView.HideSelection = false;
        _listView.MultiSelect = false;
        _listView.Columns.Add("Название", 380);
        _listView.Columns.Add("Срок", 110);
        _listView.Resize += (_, _) => ResizeColumns();

        // Ряд внизу: кнопки и статус
        _renameButton.Text = "Переименовать";
        _renameButton.Dock = DockStyle.Fill;
        _renameButton.Click += RenameButton_Click;

        _deleteButton.Text = "Удалить";
        _deleteButton.Dock = DockStyle.Fill;
        _deleteButton.Click += DeleteButton_Click;

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleRight;

        var bottomRow = CreateRow(3, new ColumnStyle(SizeType.Absolute, 120),
                                     new ColumnStyle(SizeType.Absolute, 100),
                                     new ColumnStyle(SizeType.Percent, 100));
        bottomRow.Controls.Add(_renameButton, 0, 0);
        bottomRow.Controls.Add(_deleteButton, 1, 0);
        bottomRow.Controls.Add(_statusLabel, 2, 0);

        // Всё вместе
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(addRow, 0, 0);
        root.Controls.Add(filterRow, 0, 1);
        root.Controls.Add(_listView, 0, 2);
        root.Controls.Add(bottomRow, 0, 3);
        Controls.Add(root);

        // События подключаем после настройки, чтобы они не сработали раньше времени
        _searchBox.TextChanged += (_, _) => RefreshList();
        _filterBox.SelectedIndexChanged += (_, _) => RefreshList();
        _sortCheck.CheckedChanged += (_, _) => RefreshList();
        _listView.ItemChecked += ListView_ItemChecked;

        ResizeColumns();
    }

    private static TableLayoutPanel CreateRow(int columns, params ColumnStyle[] styles)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = columns,
            RowCount = 1
        };
        foreach (var style in styles)
            row.ColumnStyles.Add(style);
        return row;
    }

    private void ResizeColumns()
    {
        if (_listView.Columns.Count < 2)
            return;
        var width = _listView.ClientSize.Width - _listView.Columns[1].Width;
        _listView.Columns[0].Width = Math.Max(120, width);
    }

    // ---------- Обработчики событий ----------

    private void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            AddButton_Click(sender, EventArgs.Empty);
        }
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var due = ParseDue(_dueBox.Text);
            _list.Add(_titleBox.Text, due);   // пустое название: исключение из модели
            _titleBox.Clear();
            _dueBox.Clear();
            SaveAndRefresh();
            _titleBox.Focus();
        }
        catch (Exception ex) when (IsUserError(ex))
        {
            ShowError(ex.Message);
        }
    }

    private void RenameButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var item = GetSelected();
            var newTitle = PromptDialog.Ask(this, "Переименовать задачу", "Новое название:", item.Title);
            if (newTitle == null)
                return;   // нажали «Отмена»

            item.Rename(newTitle);
            SaveAndRefresh();
        }
        catch (Exception ex) when (IsUserError(ex))
        {
            ShowError(ex.Message);
        }
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var item = GetSelected();
            var answer = MessageBox.Show(this, $"Удалить задачу «{item.Title}»?", "Удаление",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            _list.Remove(item);
            SaveAndRefresh();
        }
        catch (Exception ex) when (IsUserError(ex))
        {
            ShowError(ex.Message);
        }
    }

    private void ListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_refreshing || e.Item.Tag is not TodoItem item)
            return;

        if (e.Item.Checked)
            item.Complete();
        else
            item.Reopen();

            // перерисовываем позже: из-за фильтра задача может пропасть из списка
        BeginInvoke(new Action(SaveAndRefresh));
    }

    // ---------- Вспомогательное ----------

    private TodoItem GetSelected()
    {
        if (_listView.SelectedItems.Count == 0 || _listView.SelectedItems[0].Tag is not TodoItem item)
            throw new InvalidOperationException("Сначала выберите задачу в списке");
        return item;
    }

    private static DateTime? ParseDue(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return null;

        if (!DateTime.TryParseExact(text, "d.M.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            throw new FormatException("Срок нужно ввести в виде дд.мм.гггг, например 31.10.2026");

        return date.Date;
    }

    private static bool IsUserError(Exception ex) =>
        ex is ArgumentException or FormatException or InvalidOperationException;

    private void ShowError(string message) =>
        MessageBox.Show(this, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void LoadTasks()
    {
        try
        {
            _list.Load(DataPath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            ShowError("Не удалось загрузить задачи, список пуст.\n" + ex.Message);
        }
    }

    private void SaveAndRefresh()
    {
        try
        {
            _list.Save(DataPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Не удалось сохранить задачи.\n" + ex.Message);
        }
        RefreshList();
    }

    private void RefreshList()
    {
        var selected = _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Tag : null;
        var items = _list.Query((TodoFilter)_filterBox.SelectedIndex, _searchBox.Text, _sortCheck.Checked);
        var today = DateTime.Today;

        _refreshing = true;
        _listView.BeginUpdate();
        try
        {
            _listView.Items.Clear();
            foreach (var item in items)
            {
                var row = new ListViewItem(item.Title) { Tag = item, Checked = item.IsDone };
                row.SubItems.Add(item.Due?.ToString("dd.MM.yyyy") ?? "");

                if (item.IsDone)
                {
                    row.ForeColor = Color.Gray;
                    row.Font = _doneFont;
                }
                else if (item.IsOverdue(today))
                {
                    row.BackColor = Color.MistyRose;
                    row.ForeColor = Color.DarkRed;
                }

                _listView.Items.Add(row);
                if (ReferenceEquals(item, selected))
                    row.Selected = true;
            }
        }
        finally
        {
            _listView.EndUpdate();
            _refreshing = false;
        }

        _statusLabel.Text = $"Всего: {_list.Count}, активных: {_list.ActiveCount}, показано: {items.Count}";
    }
}
