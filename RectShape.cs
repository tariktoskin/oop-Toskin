namespace PaintApp;

public class RectShape : Shape
{
    public RectShape(Point start, Color color, int thickness) : base(start, color, thickness)
    {
    }

    public override string TypeName => "rect";

    public override void Draw(Graphics g)
    {
        using var pen = CreatePen();
        g.DrawRectangle(pen, Bounds);
    }

    public override bool Contains(Point p)
    {
        var area = Bounds;
        area.Inflate((int)Tolerance, (int)Tolerance);
        return area.Contains(p);
    }
}
