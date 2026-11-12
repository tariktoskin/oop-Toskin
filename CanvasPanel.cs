namespace PaintApp;

// Холст: обычная панель с двойной буферизацией, чтобы рисунок не мерцал.
public class CanvasPanel : Panel
{
    public CanvasPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }
}
