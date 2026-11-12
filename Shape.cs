using System.Drawing.Drawing2D;

namespace PaintApp;

// Абстрактный класс: общее для всех фигур. Создать Shape напрямую нельзя.
public abstract class Shape
{
    protected Shape(Point start, Color color, int thickness)
    {
        if (thickness < 1)
            throw new ArgumentException("Толщина должна быть не меньше 1");

        Start = start;
        End = start;
        Color = color;
        Thickness = thickness;
    }

    public Point Start { get; private set; }
    public Point End { get; protected set; }
    public Color Color { get; }
    public int Thickness { get; }

    // имя типа для файла и фабрики
    public abstract string TypeName { get; }

    // каждая фигура рисует себя сама
    public abstract void Draw(Graphics g);

    // попадает ли точка в фигуру (для выделения)
    public abstract bool Contains(Point p);

    public virtual Rectangle Bounds => Normalize(Start, End);

    // фигура нулевого размера не добавляется в рисунок
    public virtual bool IsDegenerate => Start == End;

    // мышь тянется: меняем вторую точку
    public virtual void MoveEnd(Point p) => End = p;

    public virtual void Move(int dx, int dy)
    {
        Start = new Point(Start.X + dx, Start.Y + dy);
        End = new Point(End.X + dx, End.Y + dy);
    }

    public virtual ShapeData ToData() => new ShapeData
    {
        Type = TypeName,
        Color = Color.ToArgb(),
        Thickness = Thickness,
        X1 = Start.X,
        Y1 = Start.Y,
        X2 = End.X,
        Y2 = End.Y
    };

    // восстановить то, что не задаёт конструктор (вторая точка)
    public virtual void ApplyData(ShapeData data) => End = new Point(data.X2, data.Y2);

    protected Pen CreatePen() => new Pen(Color, Thickness)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round
    };

    // насколько далеко от линии можно щёлкнуть и всё равно попасть
    protected double Tolerance => Thickness / 2.0 + 4;

    // тянуть можно в любую сторону: левый верхний угол это минимумы координат
    protected static Rectangle Normalize(Point a, Point b) =>
        new Rectangle(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    // расстояние от точки p до отрезка a-b
    protected static double DistanceToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double lengthSquared = dx * dx + dy * dy;

        double t = 0;
        if (lengthSquared > 0)
            t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);

        double nearestX = a.X + t * dx;
        double nearestY = a.Y + t * dy;
        return Math.Sqrt((p.X - nearestX) * (p.X - nearestX) + (p.Y - nearestY) * (p.Y - nearestY));
    }
}
