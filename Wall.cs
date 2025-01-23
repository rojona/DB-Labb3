using System.Windows.Media;

namespace NET24_Labb2_WPF;

public class Wall : LevelElement
{
    public Wall(int x, int y) : base(x, y, '#', Colors.DarkGray)
    { }
}