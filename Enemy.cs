using Color = System.Windows.Media.Color;

namespace NET24_Labb2_WPF;

abstract class Enemy : LevelElement
{
    protected LevelData Level { get; private set; }
    
    public string Name { get; set; }
    public int Health { get; set; }
    public Dice AttackDice { get; set; }
    public Dice DefenceDice { get; set; }

    protected Enemy(LevelData level, int x, int y, char symbol, Color color) : base(x, y, symbol, color)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
    }

    public abstract void Update();
}