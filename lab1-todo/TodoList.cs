using System.Text.Json;

namespace TodoApp;

public enum TodoFilter
{
    All,
    Active,
    Done
}

// так задача хранится в JSON-файле
internal record TodoItemData(string? Title, bool IsDone, DateTime? Due);

public class TodoList
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly List<TodoItem> _items = new();

    public IReadOnlyList<TodoItem> Items => _items;
    public int Count => _items.Count;
    public int ActiveCount => _items.Count(i => !i.IsDone);

    public TodoItem Add(string? title, DateTime? due = null)
    {
        var item = new TodoItem(title, due);
        _items.Add(item);
        return item;
    }

    public void Remove(TodoItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.Remove(item))
            throw new InvalidOperationException("Такой задачи нет в списке");
    }

    public List<TodoItem> Filter(TodoFilter filter) => Query(filter);

    public List<TodoItem> Search(string? text) => Query(search: text);

    public List<TodoItem> SortByDue() => Query(sortByDue: true);

    // фильтр, поиск и сортировка вместе, его вызывает окно
    public List<TodoItem> Query(
        TodoFilter filter = TodoFilter.All,
        string? search = null,
        bool sortByDue = false)
    {
        IEnumerable<TodoItem> result = _items;

        if (filter == TodoFilter.Active)
            result = result.Where(i => !i.IsDone);
        else if (filter == TodoFilter.Done)
            result = result.Where(i => i.IsDone);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            result = result.Where(i => i.Title.Contains(text, StringComparison.CurrentCultureIgnoreCase));
        }

        if (sortByDue)
            result = result.OrderBy(i => i.Due ?? DateTime.MaxValue);   // без срока в конец

        return result.ToList();
    }

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var data = _items
            .Select(i => new TodoItemData(i.Title, i.IsDone, i.Due))
            .ToList();
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
    }

    // нет файла: список остаётся пустым
    public void Load(string path)
    {
        _items.Clear();
        if (!File.Exists(path))
            return;

        try
        {
            var data = JsonSerializer.Deserialize<List<TodoItemData>>(File.ReadAllText(path))
                       ?? new List<TodoItemData>();

            foreach (var d in data)
            {
                var item = new TodoItem(d.Title, d.Due);
                if (d.IsDone)
                    item.Complete();
                _items.Add(item);
            }
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            _items.Clear();
            throw new InvalidDataException("Файл с задачами повреждён: " + ex.Message, ex);
        }
    }
}
