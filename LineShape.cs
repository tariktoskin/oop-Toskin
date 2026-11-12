namespace PaintApp;

public class LineShape : Shape
{
    public LineShape(Point start, Color color, int thickness) : base(start, color, thickness)
    {
    }

    public override string TypeName => "line";

    public override void Draw(Graphics g)
    {
        using var pen = CreatePen();
        g.DrawLine(pen, Start, End);
    }

    public override bool Contains(Point p) => DistanceToSegment(p, Start, End) <= Tolerance;
}
