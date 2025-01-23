using Color = System.Windows.Media.Color;

namespace NET24_Labb2_WPF;

public abstract class LevelElement
{
    public int X { get; set; }
    public int Y { get; set; }
    protected char Symbol { get; set; }
    protected Color Color { get; set; }

    public LevelElement(int x, int y, char symbol, Color color)
    {
        X = x;
        Y = y;
        Symbol = symbol;
        Color = color;
    }

    public char GetSymbol() => Symbol;
    public Color GetColor() => Color;
}