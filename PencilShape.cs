namespace PaintApp;

// Карандаш хранит все точки кривой, а не только две.
public class PencilShape : Shape
{
    private readonly List<Point> _points = new();

    public PencilShape(Point start, Color color, int thickness) : base(start, color, thickness)
    {
        _points.Add(start);
    }

    public override string TypeName => "pencil";

    // карандаш не растягивает фигуру, а добавляет точку
    public override void MoveEnd(Point p)
    {
        _points.Add(p);
        base.MoveEnd(p);
    }

    public override void Move(int dx, int dy)
    {
        base.Move(dx, dy);
        for (int i = 0; i < _points.Count; i++)
            _points[i] = new Point(_points[i].X + dx, _points[i].Y + dy);
    }

    // даже одна точка это уже рисунок (точка)
    public override bool IsDegenerate => false;

    public override Rectangle Bounds
    {
        get
        {
            int minX = _points.Min(p => p.X);
            int minY = _points.Min(p => p.Y);
            int maxX = _points.Max(p => p.X);
            int maxY = _points.Max(p => p.Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public override void Draw(Graphics g)
    {
        if (_points.Count == 1)
        {
            using var brush = new SolidBrush(Color);
            var p = _points[0];
            g.FillEllipse(brush, p.X - Thickness / 2f, p.Y - Thickness / 2f, Thickness, Thickness);
            return;
        }

        using var pen = CreatePen();
        g.DrawLines(pen, _points.ToArray());
    }

    public override bool Contains(Point p)
    {
        if (_points.Count == 1)
            return DistanceToSegment(p, _points[0], _points[0]) <= Tolerance;

        for (int i = 1; i < _points.Count; i++)
        {
            if (DistanceToSegment(p, _points[i - 1], _points[i]) <= Tolerance)
                return true;
        }
        return false;
    }

    public override ShapeData ToData()
    {
        var data = base.ToData();
        data.Points = _points.Select(p => new PointData(p.X, p.Y)).ToList();
        return data;
    }

    public override void ApplyData(ShapeData data)
    {
        base.ApplyData(data);

        _points.Clear();
        if (data.Points != null)
            _points.AddRange(data.Points.Select(p => new Point(p.X, p.Y)));
        if (_points.Count == 0)
            _points.Add(Start);
    }
}
