namespace PaintApp;

// Фабрика: единственное место, где по имени типа создаётся фигура.
// Новая фигура добавляется одной строкой в словарь.
public static class ShapeFactory
{
    private static readonly Dictionary<string, Func<Point, Color, int, Shape>> Creators = new()
    {
        ["line"] = (p, c, t) => new LineShape(p, c, t),
        ["rect"] = (p, c, t) => new RectShape(p, c, t),
        ["ellipse"] = (p, c, t) => new EllipseShape(p, c, t),
        ["pencil"] = (p, c, t) => new PencilShape(p, c, t),
    };

    // создать фигуру по имени типа (так делает мышь)
    public static Shape Create(string type, Point start, Color color, int thickness)
    {
        if (!Creators.TryGetValue(type, out var create))
            throw new ArgumentException($"Неизвестный тип фигуры: {type}");
        return create(start, color, thickness);
    }

    // восстановить фигуру из данных файла
    public static Shape FromData(ShapeData? data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var shape = Create(data.Type, new Point(data.X1, data.Y1), Color.FromArgb(data.Color), data.Thickness);
        shape.ApplyData(data);
        return shape;
    }
}
