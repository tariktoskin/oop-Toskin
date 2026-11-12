using System.Text.Json;

namespace PaintApp;

// Рисунок: список фигур (композиция, список лежит внутри как поле).
public class Drawing
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly List<Shape> _shapes = new();

    public IReadOnlyList<Shape> Shapes => _shapes;
    public int Count => _shapes.Count;

    public void Add(Shape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        _shapes.Add(shape);
    }

    // вернуть фигуру на прежнее место (нужно для отмены удаления)
    public void Insert(int index, Shape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (index < 0 || index > _shapes.Count)
            index = _shapes.Count;
        _shapes.Insert(index, shape);
    }

    public bool Remove(Shape shape) => _shapes.Remove(shape);

    public int IndexOf(Shape shape) => _shapes.IndexOf(shape);

    public void Clear() => _shapes.Clear();

    // один цикл для всех фигур, без проверки типа: это и есть полиморфизм
    public void Draw(Graphics g)
    {
        foreach (var shape in _shapes)
            shape.Draw(g);
    }

    // верхняя фигура под точкой (рисуются по порядку, значит последняя сверху)
    public Shape? HitTest(Point p)
    {
        for (int i = _shapes.Count - 1; i >= 0; i--)
        {
            if (_shapes[i].Contains(p))
                return _shapes[i];
        }
        return null;
    }

    public void Save(string path)
    {
        var data = _shapes.Select(s => s.ToData()).ToList();
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
    }

    public void Load(string path)
    {
        List<Shape> loaded;
        try
        {
            var data = JsonSerializer.Deserialize<List<ShapeData>>(File.ReadAllText(path))
                       ?? new List<ShapeData>();
            loaded = data.Select(ShapeFactory.FromData).ToList();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new InvalidDataException("Файл повреждён или это не рисунок: " + ex.Message, ex);
        }

        // меняем рисунок только если весь файл прочитан без ошибок
        _shapes.Clear();
        _shapes.AddRange(loaded);
    }
}
