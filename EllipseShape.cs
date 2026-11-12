namespace PaintApp;

public class EllipseShape : Shape
{
    public EllipseShape(Point start, Color color, int thickness) : base(start, color, thickness)
    {
    }

    public override string TypeName => "ellipse";

    public override void Draw(Graphics g)
    {
        using var pen = CreatePen();
        g.DrawEllipse(pen, Bounds);
    }

    // точка внутри эллипса: ((x-cx)/rx)^2 + ((y-cy)/ry)^2 <= 1
    public override bool Contains(Point p)
    {
        var box = Bounds;
        double rx = box.Width / 2.0 + Tolerance;
        double ry = box.Height / 2.0 + Tolerance;
        double cx = box.X + box.Width / 2.0;
        double cy = box.Y + box.Height / 2.0;

        double nx = (p.X - cx) / rx;
        double ny = (p.Y - cy) / ry;
        return nx * nx + ny * ny <= 1;
    }
}
