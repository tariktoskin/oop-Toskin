namespace PaintApp;

// Паттерн «Команда»: каждое действие это объект, который умеет выполниться и отмениться.
public interface ICommand
{
    void Execute();
    void Undo();
}

public class AddShapeCommand : ICommand
{
    private readonly Drawing _drawing;
    private readonly Shape _shape;

    public AddShapeCommand(Drawing drawing, Shape shape)
    {
        _drawing = drawing;
        _shape = shape;
    }

    public void Execute() => _drawing.Add(_shape);

    public void Undo() => _drawing.Remove(_shape);
}

public class RemoveShapeCommand : ICommand
{
    private readonly Drawing _drawing;
    private readonly Shape _shape;
    private int _index = -1;

    public RemoveShapeCommand(Drawing drawing, Shape shape)
    {
        _drawing = drawing;
        _shape = shape;
    }

    public void Execute()
    {
        _index = _drawing.IndexOf(_shape);   // запоминаем место, чтобы вернуть туда же
        _drawing.Remove(_shape);
    }

    public void Undo() => _drawing.Insert(_index, _shape);
}

public class ClearCommand : ICommand
{
    private readonly Drawing _drawing;
    private List<Shape> _backup = new();

    public ClearCommand(Drawing drawing) => _drawing = drawing;

    public void Execute()
    {
        _backup = _drawing.Shapes.ToList();   // копия, чтобы можно было вернуть
        _drawing.Clear();
    }

    public void Undo()
    {
        foreach (var shape in _backup)
            _drawing.Add(shape);
    }
}

// История: два стека, «можно отменить» и «можно повторить».
public class CommandHistory
{
    private readonly Stack<ICommand> _undo = new();
    private readonly Stack<ICommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Run(ICommand command)
    {
        command.Execute();
        _undo.Push(command);
        _redo.Clear();   // после нового действия повторять нечего
    }

    public void Undo()
    {
        if (!CanUndo)
            return;
        var command = _undo.Pop();
        command.Undo();
        _redo.Push(command);
    }

    public void Redo()
    {
        if (!CanRedo)
            return;
        var command = _redo.Pop();
        command.Execute();
        _undo.Push(command);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
