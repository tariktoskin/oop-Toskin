namespace TodoApp;

public class TodoItem
{
    private string _title = "";
    private bool _done;
    private DateTime? _due;

    public TodoItem(string? title, DateTime? due = null)
    {
        Rename(title);
        _done = false;
        _due = due?.Date;
    }

    public string Title => _title;
    public bool IsDone => _done;
    public DateTime? Due => _due;

    public void Complete() => _done = true;

    public void Reopen() => _done = false;

    public void Rename(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название не может быть пустым");
        _title = title.Trim();
    }

    public void SetDue(DateTime? due) => _due = due?.Date;

    // срок прошёл, а задача не выполнена
    public bool IsOverdue(DateTime today) =>
        !_done && _due.HasValue && _due.Value < today.Date;
}
