namespace PaintApp;

// Так фигура записывается в JSON-файл.
public class ShapeData
{
    public string Type { get; set; } = "";
    public int Color { get; set; }
    public int Thickness { get; set; }
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
    public List<PointData>? Points { get; set; }   // только у карандаша
}

public record PointData(int X, int Y);
